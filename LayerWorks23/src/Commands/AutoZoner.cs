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
using LoaderCore.Utilities;
using LoaderCore.UI;

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
                         IBuffer bufferizer,
                         ILayerChecker checker,
                         DrawOrderService drawOrderService,
                         IEntityPropertyRecognizer<Entity, string?>? diameterRecognizer)
        {
            _bufferizer = bufferizer;
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
                    AskForOptions(out bool isZoneChoiceNeeded, out bool ignoreLabelRecognition, out bool calculateSinglePipe);

                    List<ErrorEntry> unprocessedLayers = new();

                    foreach (var wrapper in wrappers)
                    {
                        // Проверить, есть ли зоны для слоя
                        bool success = _zoneRepository.TryGet(wrapper.LayerInfo.TrueName, out ZoneInfo[]? zoneInfos);
                        if (!success)
                        {
                            string layer = wrapper.LayerInfo.Name;
                            string message = $"Нет зон для слоя {layer}";
                            Workstation.Logger?.LogDebug("{ProcessingObject}: {Message}", nameof(AutoZoner), message);
                            unprocessedLayers.Add(new(layer, message));
                            continue;
                        }
                        string layerName = wrapper.LayerInfo.Name;

                        // Попытка получить диаметр из LayerInfo
                        bool diameterGetSuccess = TryGetDiameterFromLayerInfo(wrapper.LayerInfo, out double constructionWidth);

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
                            {
                                Workstation.Logger?.LogDebug("{ProcessingObject}: Зона {Zone} от слоя {Layer} не отбивается по заданному фильтру",
                                                             nameof(AutoZoner),
                                                             zi.ZoneLayer,
                                                             layerName);
                                continue;
                            }
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
                                entityZoneParameters.Add(new(wrapper, zi));
                            }
                            else if (diameterGetSuccess)
                            {
                                // Если ранее найден из LayerInfo - добавить статический
                                entityZoneParameters.Add(new(wrapper, zi, constructionWidth));
                            }
                            else if (_diameterRecognizer != null && !ignoreLabelRecognition)
                            {
                                // Если не найден - попытаться найти из подписейподписи через буферы
                                var ezp = GetZoneParametersByMtextLabels(wrapper, zi, calculateSinglePipe);
                                entityZoneParameters.Add(ezp);
                            }
                            else
                            {
                                entityZoneParameters.Add(new(wrapper, zi, zi.DefaultConstructionWidth));
                            }
                        }
                    }

                    // При необходимости выбрать зоны
                    IEnumerable<string>? enabledZoneNames = null;
                    if (isZoneChoiceNeeded)
                    {
                        var window = new ZonesChoiceWindow(zoneToLayersMap.Keys);
                        window.ShowDialog();
                        enabledZoneNames = window.EnabledZones.AsEnumerable();
                    }

                    List<Entity> entitiesToDraw = new();

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

                            // Или выбрать квп из словаря,
                            // или создать по статической ширине.
                            // Также привести к нужной ширине буфера - половина ширины конструкции плюс ширина зоны
                            Func<EntityZoneParameters, IEnumerable<KeyValuePair<Entity, double>>> kvpFromEzpFunc =
                                ezp => ezp.DynamicWidth?.Select(kvp => new KeyValuePair<Entity, double>(kvp.Key, kvp.Value / 2 + ezp.ZoneInfo.Value)) ??
                                ezp.Entities.Select(e => new KeyValuePair<Entity, double>(e, ezp.StaticWidth!.Value / 2 + ezp.ZoneInfo.Value));
                            // Объединить в один словарь
                            Dictionary<Entity, double> dictionary = new(ezps.SelectMany(kvpFromEzpFunc));
                            // Отбить зоны
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

                        if (!buffers.Any())
                        {
                            string layer = zoneName;
                            string message = $"Нет объектов для создания зон в слоях {string.Join(", ", ezps.Select(e => e.SourceLayerName))}";
                            Workstation.Logger?.LogDebug("{ProcessingObject}: {Message}", nameof(AutoZoner), message);
                            unprocessedLayers.Add(new(layer, message));
                            continue;
                        }

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

                    if (unprocessedLayers.Any())
                    {
                        var errorsWindow = new ErrorListWindow(unprocessedLayers, "Необработанные слои");
                        errorsWindow.ShowDialog();
                    }
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
                }
            }
        }

        private static void AskForOptions(out bool isZoneChoiceNeeded, out bool ignoreLabelRecognition, out bool calculateSinglePipe)
        {
            var window = new AutoZonerOptionsWindow();
            window.ShowDialog();
            isZoneChoiceNeeded = window.IsZoneChoiceNeeded;
            ignoreLabelRecognition = window.IgnoreLabelRecognition;
            calculateSinglePipe = window.CalculateSinglePipe;
        }

        private static void AskForOptionsInCommandLine(out bool isZoneChoiceNeeded, out bool isLabelRecognizeAttemptNeeded, out bool calculateSinglePipe)
        {
            // Не используется, так как найдено неудобным. Оставлено, на случай, если в будущем появится серьёзная проблема с WPF
            isZoneChoiceNeeded = false;
            isLabelRecognizeAttemptNeeded = true;
            calculateSinglePipe = false;
            PromptKeywordOptions pko = new("При необходимости выберите дополнительные опции [Продолжить/Выбрать/Диаметры/Однотрубный] <Продолжить>", "Продолжить Выбрать Диаметры Однотрубный")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult result = Workstation.Editor.GetKeywords(pko);
            while (result.StringResult != "Продолжить")
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
                    case "Однотрубный":
                        calculateSinglePipe = !calculateSinglePipe;
                        Workstation.Editor.WriteMessage(calculateSinglePipe ?
                            "Расчёт диаметров многотрубных линий: линия - труба" :
                            "Расчёт диаметров многотрубных линий: линия - ось многотрубной линии");
                        result = Workstation.Editor.GetKeywords(pko);
                        break;
                }
            }
        }

        private static bool TryGetDiameterFromLayerInfo(LayerInfo layerInfo, out double diameter)
        {
            bool diameterGetSuccess = layerInfo.AuxilaryData.TryGetValue("Diameter", out string? constructionWidthString) && constructionWidthString != null;
            if (diameterGetSuccess)
            {
                bool diameterParseSuccess = double.TryParse(constructionWidthString!, out double parsedNumber);
                if (diameterParseSuccess)
                {
                    diameter = parsedNumber * 0.001d;
                    return diameterParseSuccess;
                }
            }
            diameter = 0;
            return false;
        }

        private bool TryParseDiameter(string diameterString, out double constructionWidth, bool calculateSinglePipe = false)
        {
            var match = _diameterRegex.Match(diameterString);
            if (match.Success)
            {
                string miltiplierGroup = match.Groups[1].Value;
                double multiplier = string.IsNullOrEmpty(miltiplierGroup) || calculateSinglePipe ? 1d : double.Parse(match.Groups[1].Value);
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

        private EntityZoneParameters GetZoneParametersByMtextLabels(EntityLayerWrapper wrapper, ZoneInfo zoneInfo, bool calculateSinglePipe)
        {
            Dictionary<Entity, string?> diameterStringDictionary = _diameterRecognizer!.RecognizeProperty(wrapper.BoundEntities);
            string layerName = wrapper.LayerInfo.Name;
            if (diameterStringDictionary.All(p => p.Value == null))
            {
                // Если ничего не найдено - назначить по умолчанию
                return new(wrapper, zoneInfo, zoneInfo.DefaultConstructionWidth);
            }
            else
            {
                // Если подписи найдены - парсить
                var widthDictionary = new Dictionary<Entity, double>();
                foreach (var kvp in diameterStringDictionary)
                {
                    if (kvp.Value == null)
                    {
                        widthDictionary[kvp.Key] = zoneInfo.DefaultConstructionWidth;
                    }
                    else
                    {
                        // Парсить и найти максимальное значение
                        double value = 0;
                        string[] labels = kvp.Value.Split(", ");
                        foreach (var label in labels)
                        {
                            bool labelParseSuccess = TryParseDiameter(label, out var parsedLabelValue, calculateSinglePipe);
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
                            widthDictionary[kvp.Key] = zoneInfo.DefaultConstructionWidth;
                        }
                    }
                }
                return new(wrapper, zoneInfo, widthDictionary);
            }
        }

        private class EntityZoneParameters
        {
            public EntityZoneParameters(EntityLayerWrapper wrapper, ZoneInfo zoneInfo)
            {
                SourceLayerName = wrapper.LayerInfo.Name;
                ZoneInfo = zoneInfo;
                StaticWidth = 0d;
                Entities = wrapper.BoundEntities;
            }

            public EntityZoneParameters(EntityLayerWrapper wrapper, ZoneInfo zoneInfo, double staticWidth) : this(wrapper, zoneInfo)
            {
                StaticWidth = zoneInfo.IgnoreConstructionWidth ? 0d : staticWidth;
            }

            public EntityZoneParameters(EntityLayerWrapper wrapper, ZoneInfo zoneInfo, Dictionary<Entity, double> dynamicWidth) : this(wrapper, zoneInfo)
            {
                if (dynamicWidth.Any())
                {
                    DynamicWidth = dynamicWidth;
                }
                else if (zoneInfo.IgnoreConstructionWidth)
                {
                    StaticWidth = 0d;
                }
                else
                {
                    StaticWidth = zoneInfo.DefaultConstructionWidth;
                }
            }

            public string SourceLayerName { get; }
            public ZoneInfo ZoneInfo { get; }
            public double? StaticWidth { get; }
            public Dictionary<Entity, double>? DynamicWidth { get; }
            public List<Entity> Entities { get; }
        }

    }
}
