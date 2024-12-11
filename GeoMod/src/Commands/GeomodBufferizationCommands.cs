//System
using System.Collections.Generic;
using System.Linq;
//Microsoft
using Microsoft.Extensions.Configuration;
// Nanocad
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
//Internal
using LoaderCore.NanocadUtilities;
using GeoMod.GeometryConverters;
using GeoMod.UI;
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Geometries.Utilities;

using NtsBufferOps = NetTopologySuite.Operation.Buffer;
using GeoMod.NtsServices;
using System;
using LoaderCore.Interfaces;
using LoaderCore.SharedData;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GeoMod.Commands
{
    internal class GeomodBufferizationCommands
    {
        private const string RelatedConfigurationSection = "GeoModConfiguration";

        private BufferParameters _defaultBufferParameters;
        private readonly NtsGeometryServices _geometryServices;
        private readonly IRepository<string, ZoneInfo[]> _repository;
        private readonly ILayerChecker _layerChecker;
        private readonly IEntityFormatter _entityFormatter;

        public GeomodBufferizationCommands(IConfiguration configuration,
                                           INtsGeometryServicesFactory geometryServicesFactory,
                                           IRepository<string, ZoneInfo[]> repository,
                                           ILayerChecker checker,
                                           IEntityFormatter formatter)
        {
            var section = configuration.GetRequiredSection(RelatedConfigurationSection);
            _defaultBufferParameters = section.GetValue<BufferParameters>("BufferParameters") ?? new()
            {
                EndCapStyle = EndCapStyle.Round,
                JoinStyle = NtsBufferOps.JoinStyle.Round,
                QuadrantSegments = 6,
                SimplifyFactor = 0.02d,
                IsSingleSided = false
            };
            _geometryServices = geometryServicesFactory.Create();
            _repository = repository;
            _layerChecker = checker;
            _entityFormatter = formatter;
        }

        private static double DefaultBufferDistance { get; set; } = 1d;


        /// <summary>
        /// Создать буферную зону заданной величины от выбранных объектов
        /// </summary>
        public void SimpleZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить выбранные объекты и преобразовать в геометрию nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = EntityToGeometryConverter.TransferGeometry(entities!, geometryFactory).ToArray();
                if (geometries.Length == 0)
                    return;

                // получить размер буферной зоны
                PromptDoubleOptions pdo = new("Введите размер буферной зоны [Параметры]", "Параметры")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = DefaultBufferDistance,
                    AppendKeywordsToMessage = true,
                };

                PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                while (result.Status == PromptStatus.Keyword)
                {
                    BufferParametersWindow window = new(ref _defaultBufferParameters);
                    Application.ShowModalWindow(window);
                    result = Workstation.Editor.GetDouble(pdo);
                }
                if (result.Status != PromptStatus.OK)
                    return;
                double bufferDistance = result.Value;
                DefaultBufferDistance = bufferDistance;

                // Создать геометрию буферных зон и объединить
                Geometry[] buffers = geometries.Select(g => g!.Buffer(bufferDistance, _defaultBufferParameters)).ToArray();
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // Поместить в модель
                BlockTableRecord modelSpace = Workstation.ModelSpace;
                IEnumerable<Polyline> polylines = GeometryToDwgConverter.ToDWGPolylines(union);
                foreach (Polyline pl in polylines)
                {
                    modelSpace.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Создать буферную зону от выбранных объектов с заданием величины для каждого слоя выбранных объектов
        /// </summary>
        public void DiverseZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить выбранные объекты
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();

                // Получить слои выбранных объектов
                string[] layerNames = entities.Select(e => e!.Layer).Distinct().ToArray();

                // Получить ввод пользователя - размер буферной зоны для каждого слоя, и сохранить в словаре
                Dictionary<string, double> bufferSizes = new();
                foreach (string layer in layerNames)
                {
                    PromptDoubleOptions pdo = new($"Введите размер буферной зоны для слоя {layer} [Параметры]", "Параметры")
                    {
                        AllowNegative = false,
                        AllowZero = true,
                        AllowNone = false,
                        AppendKeywordsToMessage = true
                    };
                    PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                    if (result.Status == PromptStatus.Cancel)
                        return;
                    while (result.Status == PromptStatus.Keyword)
                    {
                        if (result.StringResult == "Параметры")
                        {
                            BufferParametersWindow window = new(ref _defaultBufferParameters);
                            Application.ShowModalWindow(window);
                        }
                        result = Workstation.Editor.GetDouble(pdo);
                    }
                    if (result.Status != PromptStatus.OK || result.Value == 0)
                        continue;
                    bufferSizes[layer] = result.Value;
                }

                // Создать геометрию буферных зон и объединить
                var buffers = (from Entity entity in entities
                               where entity is Polyline
                               let pl = entity as Polyline
                               let buffer = pl.ToNTSGeometry(geometryFactory).Buffer(bufferSizes[pl.Layer], _defaultBufferParameters) as Polygon
                               select buffer).ToArray();
                if (buffers.Length == 0)
                    return;
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // Поместить в модель
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in GeometryToDwgConverter.ToDWGPolylines(union))
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        public void AutoZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить выбранные объекты
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Func<Entity, Geometry?> convert = e => EntityToGeometryConverter.TransferGeometry(e, geometryFactory);
                //var entities = psr.Value.GetObjectIds().Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead));

                var entities = psr.Value.GetObjectIds()
                             .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                             .GroupBy(e => e.Layer);

                // Получить слои выбранных объектов
                string[] layerNames = entities.Select(e => e!.Key).ToArray();
                List<ZoneFeature> features = new();
                DynamicRoundBufferParametersProvider provider = new(1d, 4, 30d, 16); // TODO: перенести в конфигурацию

                foreach (var layer in entities)
                {
                    string key = Regex.Replace(layer.Key, @"^[^_\s-\.]+[_\s-\.]", ""); // TODO: изменить логику на чтение имён с префиксами
                    bool success = _repository.TryGet(key, out ZoneInfo[]? zoneInfos);
                    if (success)
                    {
                        foreach (var zoneInfo in zoneInfos!)
                        {
                            Geometry[] geometries = EntityToGeometryConverter.TransferGeometry(layer.ToArray(), geometryFactory).ToArray();
                            double width = zoneInfo.Value + zoneInfo.DefaultConstructionWidth;
                            BufferParameters parameters = provider.GetBufferParameters(width);
                            Geometry[] buffer = geometries.Select(g => g.Buffer(width, parameters)).ToArray();
                            // добавление слоя зоны
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
                            // создание фичи
                            ZoneFeature feature = new()
                            {
                                Layer = zoneLayer,
                                Geometry = geometryFactory.CreateGeometryCollection(buffer).Union()
                            };
                        }
                    }
                }
                ZoneFeature[] buffers = features.GroupBy(f => f.Layer).Select(f => ZoneFeature.Combine(f.ToArray(), geometryFactory)).ToArray();

                BlockTableRecord modelSpace = Workstation.ModelSpace;
                LayerTable layerTable = (LayerTable)transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead);
                ObjectId initialClayer = Workstation.Database.Clayer;
                foreach (var feature in buffers)
                {
                    Workstation.Database.Clayer = layerTable[feature.Layer];
                    Polyline[] polylines = GeometryToDwgConverter.ToDWGPolylines(feature.Geometry).ToArray();
                    foreach (var polyline in polylines)
                    {
                        modelSpace.AppendEntity(polyline);
                        transaction.AddNewlyCreatedDBObject(polyline, true);
                    }
                    // TODO: проверить, нужна ли вообще штриховка
                    Hatch hatch = new()
                    {
                        Layer = feature.Layer,
                        HatchStyle = HatchStyle.Normal
                    };
                    hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(polylines.Select(p => p.Id).ToArray()));
                    _entityFormatter.FormatEntity(hatch);
                    modelSpace.AppendEntity(hatch);
                    transaction.AddNewlyCreatedDBObject(hatch, true);
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


        public void ZoneJoin()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить выбранные объекты, отфильтровать замкнутые полилинии и создать из них nts полигоны
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var closedPolylines = (from ObjectId id in entitiesIds
                                       let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                       where entity is Polyline pl && pl.Closed
                                       select entity as Polyline).ToArray();
                // Вывести предупреждения о необработанных объектах
                if (closedPolylines.Length == 0)
                {
                    Workstation.Editor.WriteMessage("Нет подходящих замкнутых полилиний в наборе");
                    return;
                }
                if (closedPolylines.Length < entitiesIds.Length)
                    Workstation.Editor.WriteMessage($"Не обработано {entitiesIds.Length - closedPolylines.Length} объектов, не являющихся замкнутыми полилиниями");

                // Провести валидацию геометрии
                Polygon?[] polygons = closedPolylines.Select(pl => pl.ToNTSGeometry(geometryFactory) as Polygon).ToArray();
                Geometry?[] fixedPolygons = polygons.Select(g => g!.IsValid ? g : GeometryFixer.Fix(g)).ToArray();

                // Удалить исходные полилинии
                foreach (Polyline pl in closedPolylines)
                    pl.Erase();

                // Объединить геометрию, создать из неё полилинии и поместить в модель
                Geometry union = geometryFactory.CreateGeometryCollection(fixedPolygons).Union();

                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in GeometryToDwgConverter.ToDWGPolylines(union))
                {
                    modelSpace!.AppendEntity(pl);
                }

                transaction.Commit();
            }
        }
    }
}
