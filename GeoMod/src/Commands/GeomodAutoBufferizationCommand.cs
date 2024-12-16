//System
using System.Collections.Generic;
using System.Linq;
//Microsoft
// Nanocad
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
//Internal
using LoaderCore.NanocadUtilities;
using GeoMod.GeometryConverters;
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using GeoMod.NtsServices;
using System;
using LoaderCore.Interfaces;
using LoaderCore.SharedData;
using Microsoft.Extensions.Logging;
using GeoMod.Processing;

namespace GeoMod.Commands
{
    internal class GeomodAutoBufferizationCommand
    {
        private readonly NtsGeometryServices _geometryServices;
        private readonly IRepository<string, ZoneInfo[]> _repository;
        private readonly ILayerChecker _layerChecker;
        private readonly IEntityFormatter _entityFormatter;

        public GeomodAutoBufferizationCommand(INtsGeometryServicesFactory geometryServicesFactory,
                                       IRepository<string, ZoneInfo[]> repository,
                                       ILayerChecker checker,
                                       IEntityFormatter formatter)
        {
            _geometryServices = geometryServicesFactory.Create();
            _repository = repository;
            _layerChecker = checker;
            _entityFormatter = formatter;
        }

        public void AutoZone()
        {
            // TODO: добавить логгирование
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить выбранные объекты
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;

                // TODO: как-то использовать TrueName
                // Группировать по слою
                var entities = psr.Value.GetObjectIds()
                             .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                             .GroupBy(e => e.Layer);

                List<ZoneFeature> features = new();
                DynamicRoundBufferParametersProvider provider = new(1d, 4, 30d, 16); // TODO: перенести в конфигурацию. Отладить провайдер параметров

                foreach (var layer in entities)
                {
                    bool success = _repository.TryGet(layer.Key, out ZoneInfo[]? zoneInfos);
                    if (success)
                    {
                        // Для каждой группы найти слои зоны
                        foreach (var zoneInfo in zoneInfos!)
                        {
                            // Для каждого слоя зоны:
                            // Перевести двг в геометрию
                            Geometry[] geometries = EntityToGeometryConverter.TransferGeometry(layer.ToArray(), geometryFactory).ToArray();
                            // Рассчитать ширину зоны
                            double width = zoneInfo.Value + zoneInfo.DefaultConstructionWidth; // TODO: добавить распознавание диаметров сетей
                            // Создать буферы
                            BufferParameters parameters = provider.GetBufferParameters(width);
                            Geometry[] buffer = geometries.Select(g => g.Buffer(width, parameters)).ToArray();
                            // Добавить слой зоны
                            string zoneLayer = zoneInfo.ZoneLayer;
                            try
                            {
                                _layerChecker.Check(zoneLayer);
                            }
                            catch (Exception ex)
                            {
                                Workstation.Logger?.LogDebug(ex, "{ProcessingObject}: Ошибка добавления слоя. Назначение слоя \"0\"", nameof(AutoZone));
                                zoneLayer = "0";
                            }
                            // Создать фичу с данными о слое и объединёнными буферами
                            ZoneFeature feature = new()
                            {
                                Layer = zoneLayer,
                                Geometry = geometryFactory.CreateGeometryCollection(buffer).Union()
                            };
                            features.Add(feature);
                        }
                    }
                }
                ZoneFeature[] buffers = features.GroupBy(f => f.Layer).Select(f => ZoneFeature.Combine(f.ToArray(), geometryFactory)).ToArray();

                BlockTableRecord modelSpace = Workstation.ModelSpace;
                LayerTable layerTable = (LayerTable)transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead);
                ObjectId initialClayer = Workstation.Database.Clayer;

                // Для каждой фичи перевести зону в двг-полилинии, отформатировать, при необходимости - заштриховать и отформатировать штриховку
                foreach (var feature in buffers)
                {
                    Workstation.Database.Clayer = layerTable[feature.Layer];
                    Polyline[] polylines = GeometryToDwgConverter.ToDWGPolylines(feature.Geometry).ToArray();
                    foreach (var polyline in polylines)
                    {
                        modelSpace.AppendEntity(polyline);
                        transaction.AddNewlyCreatedDBObject(polyline, true);
                    }
                    Hatch hatch = new()
                    {
                        Layer = feature.Layer,
                        HatchStyle = HatchStyle.Normal
                    };
                    hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(polylines.Select(p => p.Id).ToArray()));
                    _entityFormatter.FormatEntity(hatch);
                    // Если параметры не нашлись - не добавлять штриховку в чертёж
                    if (hatch.PatternName != "")
                    {
                        modelSpace.AppendEntity(hatch);
                        transaction.AddNewlyCreatedDBObject(hatch, true);
                    }
                }
                Workstation.Database.Clayer = initialClayer;

                transaction.Commit();
            }
        }


        private class ZoneFeature
        {
            public ZoneFeature() { }

            public ZoneFeature(string layer, Geometry geometry)
            {
                Layer = layer;
                Geometry = geometry;
            }

            public string Layer { get; set; }
            public Geometry Geometry { get; set; }

            public static ZoneFeature Combine(IEnumerable<ZoneFeature> features, GeometryFactory factory)
            {
                var geometry = factory.CreateGeometryCollection(features.Select(f => f.Geometry).ToArray()).Union();
                var layer = features.First().Layer;
                return new(layer, geometry);
            }
        }
    }
}
