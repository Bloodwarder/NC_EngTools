using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Teigha.DatabaseServices;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using LoaderCore.SharedData;
using NameClassifiers;

namespace LayerWorks.Commands
{
    internal class AutoZoner
    {
        private readonly IBuffer _bufferizer;
        private readonly IRepository<string, ZoneInfo[]> _zoneRepository;
        private readonly IEntityFormatter _entityFormatter;
        private readonly ILayerChecker _checker;
        private readonly DrawOrderService _drawOrderService;
        public AutoZoner(IEntityFormatter entityFormatter,
                         IRepository<string, ZoneInfo[]> repository,
                         IBuffer bufferizator,
                         ILayerChecker checker,
                         DrawOrderService drawOrderService)
        {
            _bufferizer = bufferizator;
            _entityFormatter = entityFormatter;
            _zoneRepository = repository;
            _checker = checker;
            _drawOrderService = drawOrderService;
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
                    List<KeyValuePair<string, ZoneInfo>> layersToZonePairs = new(); // можно было бы просто кортежем - квп в этом виде не используется

                    // Найти соответствия имён присутствующих слоёв с зонами и слоёв зон с присутствующими слоями

                    Regex additionalFilterRegex = new(@"(\^?)([^_\-\.\ ]+)");

                    foreach (var wrapper in wrappers)
                    {
                        // Проверить, есть ли зоны для слоя
                        bool success = _zoneRepository.TryGet(wrapper.LayerInfo.TrueName, out ZoneInfo[]? zoneInfos);
                        if (!success)
                            continue;
                        string layerName = wrapper.LayerInfo.Name;

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
                            layersToZonePairs.Add(new KeyValuePair<string, ZoneInfo>(layerName, zi));
                        }
                    }

                    List<Entity> entitiesToDraw = new();

                    foreach (string zoneName in zoneToLayersMap.Keys)
                    {
                        // Проверить/добавить слой
                        _checker.Check(zoneName);

                        // Создать словарь с шириной зоны для каждого исходного слоя
                        Dictionary<string, double> widthDict = layersToZonePairs
                            .Where(kvp => kvp.Value.ZoneLayer == zoneName)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DefaultConstructionWidth + kvp.Value.Value);

                        // Выбрать объекты чертежа и создать для них буфер
                        var entities = wrappers.Where(lw => zoneToLayersMap[zoneName].Contains(lw.LayerInfo.Name))
                                               .SelectMany(lw => lw.BoundEntities);
                        var buffers = _bufferizer.Buffer(entities, widthDict, zoneName).ToArray();

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
                    // TODO: Порядок прорисовки
                    _drawOrderService.ArrangeDrawOrder(entitiesToDraw);

                    transaction.Commit();
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
                }
            }
        }
    }
}
