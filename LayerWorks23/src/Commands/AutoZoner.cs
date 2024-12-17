using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using LoaderCore.SharedData;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using Org.BouncyCastle.Bcpg.Sig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LayerWorks.Commands
{
    internal class AutoZoner
    {
        private readonly IBuffer _bufferizator;
        private readonly IRepository<string, ZoneInfo[]> _zoneRepository;
        private readonly IEntityFormatter _entityFormatter;
        private readonly ILayerChecker _checker;
        public AutoZoner(IEntityFormatter entityFormatter,
                         IRepository<string, ZoneInfo[]> repository,
                         IBuffer bufferizator,
                         ILayerChecker checker)
        {
            _bufferizator = bufferizator;
            _entityFormatter = entityFormatter;
            _zoneRepository = repository;
            _checker = checker;
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
                    foreach (var wrapper in wrappers)
                    {
                        bool success = _zoneRepository.TryGet(wrapper.LayerInfo.TrueName, out ZoneInfo[]? zoneInfos);
                        if (!success)
                            continue;
                        string layerName = wrapper.LayerInfo.Name;
                        foreach (var zi in zoneInfos!)
                        {
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

                    foreach (var zone in zoneToLayersMap.Keys)
                    {
                        _checker.Check(zone);
                        Dictionary<string, double> widthDict = layersToZonePairs
                            .Where(kvp => kvp.Value.ZoneLayer == zone)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DefaultConstructionWidth + kvp.Value.Value);

                        var entities = wrappers.Where(lw => zoneToLayersMap[zone].Contains(lw.LayerInfo.Name))
                                               .SelectMany(lw => lw.BoundEntities);
                        var buffers = _bufferizator.Buffer(entities, widthDict, zone).ToArray();

                        BlockTableRecord modelSpace = Workstation.ModelSpace;
                        foreach (var polyline in buffers)
                        {
                            modelSpace.AppendEntity(polyline);
                            transaction.AddNewlyCreatedDBObject(polyline, true);
                            _entityFormatter.FormatEntity(polyline);
                        }
                        Hatch hatch = new()
                        {
                            Layer = zone,
                            HatchStyle = HatchStyle.Normal
                        };
                        hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(buffers.Select(p => p.Id).ToArray()));
                        _entityFormatter.FormatEntity(hatch);
                        // Если параметры не нашлись - не добавлять штриховку в чертёж
                        if (hatch.PatternName != "")
                        {
                            modelSpace.AppendEntity(hatch);
                            transaction.AddNewlyCreatedDBObject(hatch, true);
                        }
                    }
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
