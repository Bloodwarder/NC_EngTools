using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Teigha.DatabaseServices;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using LoaderCore.SharedData;
using NameClassifiers;
using HostMgd.EditorInput;
using LayerWorks.UI;

namespace LayerWorks.Commands
{
    internal class AutoZoner
    {
        private const double DefaultBetweenPipesSpace = 0.4d;
        private readonly IBuffer _bufferizer;
        private readonly IRepository<string, ZoneInfo[]> _zoneRepository;
        private readonly IEntityFormatter _entityFormatter;
        private readonly ILayerChecker _checker;
        private readonly DrawOrderService _drawOrderService;
        private readonly IEntityPropertyRecognizer<Entity, string?>? _diameterRecognizer;
        private readonly Regex _diameterRegex = new(@"(\d)?(%%C(\d{1,5}))");
        public AutoZoner(IEntityFormatter entityFormatter,
                         IRepository<string, ZoneInfo[]> repository,
                         IBuffer bufferizator,
                         ILayerChecker checker,
                         DrawOrderService drawOrderService,
                         IEntityPropertyRecognizer<Entity, string?>? diameterRecognizer)
        {
            _bufferizer = bufferizator;
            _entityFormatter = entityFormatter;
            _zoneRepository = repository;
            _checker = checker;
            _drawOrderService = drawOrderService;
            _diameterRecognizer = diameterRecognizer;
        }


        internal void AutoZone()
        {
            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    // Выбрать только для объектов чертежа
                    var wrappers = LayerWrapper.ActiveWrappers.Where(lw => lw is EntityLayerWrapper).Select(lw => (EntityLayerWrapper)lw);
                    if (!wrappers.Any())
                    {
                        Workstation.Logger?.LogInformation("Не выбраны объекты");
                        return;
                    }
                    Dictionary<string, HashSet<string>> zoneToLayersMap = new();
                    List<EntityZoneParameters> entityZoneParameters = new();

                    // Найти соответствия имён присутствующих слоёв с зонами и слоёв зон с присутствующими слоями

                    Regex additionalFilterRegex = new(@"(\^?)([^_\-\.\ ]+)");

                    // Выбрать дополнительные опции
                    bool isZoneChoiceNeeded = false;
                    bool isLabelRecognizeAttemptNeeded = true;
                    PromptKeywordOptions pko = new("При необходимости выберите дополнительные опции [Продолжить/Выбрать/Диаметры] <Продолжить>", "Продолжить Выбрать Диаметры")
                    {
                        AppendKeywordsToMessage = true,
                        AllowNone = false,
                        AllowArbitraryInput = false
                    };
                    PromptResult result = Workstation.Editor.GetKeywords(pko);
                    while(result.StringResult != "Продолжить")
                    {
                        if (result.Status != PromptStatus.OK)
                            return;
                        switch (result.StringResult)
                        {
                            case "Выбрать":
                                isZoneChoiceNeeded = !isZoneChoiceNeeded;
                                Workstation.Editor.WriteMessage(isZoneChoiceNeeded ? "Выбрать состав зон" : "Отбить все зоны");
                                result = Workstation.Editor.GetKeywords(pko);
                                break;
                            case "Диаметры":
                                isLabelRecognizeAttemptNeeded = !isLabelRecognizeAttemptNeeded;
                                Workstation.Editor.WriteMessage($"Диаметры {(isLabelRecognizeAttemptNeeded ? "определяются" : "не определяются")} по подписям");
                                result = Workstation.Editor.GetKeywords(pko);
                                break;
                        }
                    }    

                    foreach (var wrapper in wrappers)
                    {
                        // Проверить, есть ли зоны для слоя
                        bool success = _zoneRepository.TryGet(wrapper.LayerInfo.TrueName, out ZoneInfo[]? zoneInfos);
                        if (!success)
                            continue;
                        string layerName = wrapper.LayerInfo.Name;

                        // Попытка получить диаметр из LayerInfo

                        bool diameterGetSuccess = wrapper.LayerInfo.AuxilaryData.TryGetValue("Diameter", out string? constructionWidthString) && constructionWidthString != null;
                        double constructionWidth = 0;
                        bool diameterParseSuccess = false;
                        if (diameterGetSuccess)
                        {
                            diameterParseSuccess = double.TryParse(constructionWidthString!, out var parsedNumber);
                            if (diameterParseSuccess)
                                constructionWidth = parsedNumber * 0.001d;
                        }

                        // Каждую зону добавить в словарь с соответствиями
                        foreach (var zi in zoneInfos!)
                        {
                            Func<string, bool>? additionalFilterPredicate = null;
                            // Если в ZoneInfo присутствует дополнительный фильтр - заменить предикат
                            if (!string.IsNullOrEmpty(zi.AdditionalFilter))
                            {
                                var match = additionalFilterRegex.Match(zi.AdditionalFilter);
                                if (match.Success)
                                {
                                    additionalFilterPredicate = match.Groups[1].Value == @"^" ?
                                                                s => !s.Contains(match.Groups[2].Value) :
                                                                s => s.Contains(match.Groups[2].Value);
                                }
                            }
                            additionalFilterPredicate ??= s => true;
                            // Если слой не подходит по дополнительному фильтру - перейти к следующему слою
                            if (!additionalFilterPredicate(layerName))
                                continue;
                            // Добавить в словари
                            if (zoneToLayersMap.ContainsKey(zi.ZoneLayer))
                            {
                                zoneToLayersMap[zi.ZoneLayer].Add(layerName);
                            }
                            else
                            {
                                zoneToLayersMap[zi.ZoneLayer] = new HashSet<string>() { layerName };
                            }

                            // Сопоставить диаметры
                            if (zi.IgnoreConstructionWidth)
                            {
                                entityZoneParameters.Add(new(layerName, zi, wrapper.BoundEntities));
                            }
                            else if (diameterParseSuccess)
                            {
                                // Если ранее найден из LayerInfo - добавить статический
                                entityZoneParameters.Add(new(layerName, zi, wrapper.BoundEntities, constructionWidth));
                            }
                            else if (_diameterRecognizer != null && isLabelRecognizeAttemptNeeded)
                            {
                                // Если не найден - попытаться найти подписи через буферы
                                var diameterStringDictionary = _diameterRecognizer.RecognizeProperty(wrapper.BoundEntities);
                                if (diameterStringDictionary.All(p => p.Value == null))
                                {
                                    // Если ничего не найдено - назначить по умолчанию
                                    entityZoneParameters.Add(new(layerName, zi, wrapper.BoundEntities, zi.DefaultConstructionWidth));
                                }
                                else
                                {
                                    // Если подписи найдены - парсить
                                    var widthDictionary = new Dictionary<Entity, double?>();
                                    foreach (var kvp in diameterStringDictionary)
                                    {
                                        if (kvp.Value == null)
                                        {
                                            widthDictionary[kvp.Key] = null;
                                        }
                                        else
                                        {
                                            // Парсить и найти максимальное значение
                                            double value = 0;
                                            string[] labels = kvp.Value.Split(", ");
                                            foreach (var label in labels)
                                            {
                                                bool labelParseSuccess = TryParseDiameter(label, out var parsedLabelValue);
                                                if (labelParseSuccess && parsedLabelValue > value)
                                                    value = parsedLabelValue;
                                            }
                                            if (value != 0)
                                            {
                                                widthDictionary[kvp.Key] = value;
                                            }
                                            else
                                            {
                                                // Если ничего не распарсилось - назначить по умолчанию
                                                widthDictionary[kvp.Key] = zi.DefaultConstructionWidth;
                                            }
                                        }
                                    }
                                    entityZoneParameters.Add(new(layerName, zi, widthDictionary));
                                }
                            }
                            else
                            {
                                entityZoneParameters.Add(new(layerName, zi, wrapper.BoundEntities, zi.DefaultConstructionWidth));
                            }
                        }
                    }

                    List<Entity> entitiesToDraw = new();

                    // При необходимости выбрать зоны
                    IEnumerable<string>? enabledZoneNames = null;
                    if (isZoneChoiceNeeded)
                    {
                        var window = new ZonesChoiceWindow(zoneToLayersMap.Keys);
                        window.ShowDialog();
                        enabledZoneNames = window.EnabledZones.AsEnumerable();
                    }

                    foreach (string zoneName in enabledZoneNames ?? zoneToLayersMap.Keys)
                    {

                        // Проверить/добавить слой
                        _checker.Check(zoneName);

                        // Создать словарь с шириной зоны для каждого исходного слоя
                        var ezps = entityZoneParameters.Where(ezp => ezp.ZoneInfo.ZoneLayer == zoneName);
                        Polyline[] buffers;
                        if (ezps.Any(ezp => ezp.DynamicWidth != null))
                        {
                            // Если есть динамическая ширина конструкции для каждого объекта, полученная из подписей

                            // Или выбрать квп из словаря, или создать по статической ширине.
                            // Также привести к нужной ширине буфера - половина ширины конструкции плюс ширина зоны
                            Func<EntityZoneParameters, IEnumerable<KeyValuePair<Entity, double>>> kvpFromEzpFunc =
                                ezp => ezp.DynamicWidth?.Select(kvp => new KeyValuePair<Entity, double>(kvp.Key, kvp.Value / 2 + ezp.ZoneInfo.Value)) ??
                                ezp.Entities.Select(e => new KeyValuePair<Entity, double>(e, ezp.StaticWidth!.Value / 2 + ezp.ZoneInfo.Value));

                            var dictionary = ezps.SelectMany(kvpFromEzpFunc).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            buffers = _bufferizer.Buffer(dictionary, zoneName).ToArray();
                        }
                        else
                        {
                            // Если же нет - обрабатываем по статической
                            Dictionary<string, double> widthDict = ezps.ToDictionary(ezp => ezp.SourceLayerName, ezp => ezp.StaticWidth!.Value / 2 + ezp.ZoneInfo.Value);
                            var entities = wrappers.Where(lw => zoneToLayersMap[zoneName].Contains(lw.LayerInfo.Name))
                                                   .SelectMany(lw => lw.BoundEntities);
                            buffers = _bufferizer.Buffer(entities, widthDict, zoneName).ToArray();
                        }
                        //Dictionary<string, double> widthDict = entityZoneParameters
                        //    .Where(ezp => ezp.ZoneInfo.ZoneLayer == zoneName)
                        //    .ToDictionary(ezp => ezp.ZoneLayerName, ezp => ezp.ZoneInfo.DefaultConstructionWidth + ezp.ZoneInfo.Value);

                        // Выбрать объекты чертежа и создать для них буфер
                        //var entities = wrappers.Where(lw => zoneToLayersMap[zoneName].Contains(lw.LayerInfo.Name))
                        //                       .SelectMany(lw => lw.BoundEntities);
                        //var buffers = _bufferizer.Buffer(entities, widthDict, zoneName).ToArray();

                        // Добавить в чертёж, отформатировать и по необходимости заштриховать полученные полилинии
                        BlockTableRecord modelSpace = Workstation.ModelSpace;
                        foreach (var polyline in buffers)
                        {
                            entitiesToDraw.Add(polyline);
                            modelSpace.AppendEntity(polyline);
                            transaction.AddNewlyCreatedDBObject(polyline, true);
                            _entityFormatter.FormatEntity(polyline);
                        }
                        Hatch hatch = new()
                        {
                            Layer = zoneName,
                            HatchStyle = HatchStyle.Normal
                        };
                        hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(buffers.Select(p => p.Id).ToArray()));
                        _entityFormatter.FormatEntity(hatch);
                        // Если параметры не нашлись - не добавлять штриховку в чертёж
                        if (hatch.PatternName != "")
                        {
                            entitiesToDraw.Add(hatch);
                            modelSpace.AppendEntity(hatch);
                            transaction.AddNewlyCreatedDBObject(hatch, true);
                        }
                    }
                    _drawOrderService.ArrangeDrawOrder(entitiesToDraw);

                    transaction.Commit();
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
                }
            }
        }

        private bool TryParseDiameter(string diameterString, out double constructionWidth)
        {
            var match = _diameterRegex.Match(diameterString);
            if (match.Success)
            {
                string miltiplierGroup = match.Groups[1].Value;
                double multiplier = string.IsNullOrEmpty(miltiplierGroup) ? 1d : double.Parse(match.Groups[1].Value);
                double diameter = double.Parse(match.Groups[3].Value) * 0.001d;
                constructionWidth = (multiplier - 1) * DefaultBetweenPipesSpace + multiplier * diameter;
                return true;
            }
            else
            {
                constructionWidth = default;
                return false;
            }
        }

        class EntityZoneParameters
        {
            public EntityZoneParameters(string sourceLayerName, ZoneInfo zoneInfo, List<Entity> entities)
            {
                SourceLayerName = sourceLayerName;
                ZoneInfo = zoneInfo;
                StaticWidth = 0d;
                Entities = entities;
            }

            public EntityZoneParameters(string zoneName, ZoneInfo zoneInfo, List<Entity> entities, double staticWidth) : this(zoneName, zoneInfo, entities)
            {
                StaticWidth = zoneInfo.IgnoreConstructionWidth ? 0d : staticWidth;
            }

            public EntityZoneParameters(string zoneName, ZoneInfo zoneInfo, Dictionary<Entity, double?> dynamicWidth) : this(zoneName, zoneInfo, dynamicWidth.Keys.ToList())
            {
                if (!zoneInfo.IgnoreConstructionWidth)
                {
                    DynamicWidth = new();
                    foreach (var kvp in dynamicWidth.AsEnumerable())
                    {
                        if (kvp.Value != null)
                            DynamicWidth[kvp.Key] = kvp.Value!.Value;
                        else
                            DynamicWidth[kvp.Key] = zoneInfo.DefaultConstructionWidth;
                    }
                }
                else
                {
                    StaticWidth = 0d;
                }
            }

            public string SourceLayerName { get; private set; }
            public ZoneInfo ZoneInfo { get; private set; }
            public double? StaticWidth { get; set; }
            public Dictionary<Entity, double>? DynamicWidth { get; set; }

            public List<Entity> Entities { get; set; }
        }

    }
}
