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
using Teigha.Geometry;

namespace GeoMod.Commands
{
    internal class GeomodBufferizationCommands
    {
        private const string RelatedConfigurationSection = "GeoModConfiguration";
        private const double DefaultReductionMultiplier = 2d;
        private BufferParameters _defaultBufferParameters;
        private readonly NtsGeometryServices _geometryServices;

        public GeomodBufferizationCommands(IConfiguration configuration, INtsGeometryServicesFactory geometryServicesFactory)
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
        /// Создать буферную зону от точки
        /// </summary>
        public void PointZone()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить выбранные объекты и преобразовать в геометрию nts
                PromptPointResult psr = Workstation.Editor.GetPoint("Укажите точку");
                if (psr.Status != PromptStatus.OK)
                    return;
                Geometry geometry = new Point(psr.Value.X, psr.Value.Y);

                // получить размер буферной зоны
                PromptDoubleOptions pdo = new("Введите размер буферной зоны")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = DefaultBufferDistance,
                    AppendKeywordsToMessage = true,
                };

                PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                if (result.Status != PromptStatus.OK)
                    return;
                double bufferDistance = result.Value;
                DefaultBufferDistance = bufferDistance;

                // Создать геометрию буферных зон и объединить
                Geometry buffer = geometry.Buffer(bufferDistance, _defaultBufferParameters);

                // Поместить в модель
                BlockTableRecord modelSpace = Workstation.ModelSpace;
                IEnumerable<Polyline> polylines = GeometryToDwgConverter.ToDWGPolylines(buffer);
                foreach (Polyline pl in polylines)
                {
                    modelSpace.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Создать сокращённую буферную зону от полилинии с полной зоной на концах
        /// </summary>
        public void ReducedLinearZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить выбранные объекты и преобразовать в геометрию nts
                PromptEntityOptions peo = new("Выберите полилинию")
                {
                    AllowNone = false
                };
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per = Workstation.Editor.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;
                Polyline polyline = per.ObjectId.GetObject<Polyline>(OpenMode.ForRead, transaction);
                Geometry lineString = EntityToGeometryConverter.TransferGeometry(polyline!, geometryFactory);
                Point2d p1 = polyline.GetPoint2dAt(0);
                Point2d p2 = polyline.GetPoint2dAt(polyline.NumberOfVertices - 1);
                Geometry startPoint = new Point(p1.X, p1.Y);
                Geometry endPoint = new Point(p2.X, p2.Y);

                // получить размер буферной зоны
                PromptDoubleOptions pdo = new("Введите размер буферной зоны на концах [Параметры]", "Параметры")
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
                double endBufferDistance = result.Value;
                DefaultBufferDistance = endBufferDistance;

                PromptDoubleOptions pdo2 = new("Введите размер буферной зоны по трассе линии [Параметры]", "Параметры")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = DefaultBufferDistance / DefaultReductionMultiplier,
                    AppendKeywordsToMessage = true,
                };
                PromptDoubleResult result2 = Workstation.Editor.GetDouble(pdo2);
                double linearBufferDistance;
                if (result2.Status != PromptStatus.OK)
                    linearBufferDistance = endBufferDistance / DefaultReductionMultiplier;
                else
                    linearBufferDistance = result2.Value;

                // Создать геометрию буферных зон и объединить
                Geometry endBuffer1 = startPoint.Buffer(endBufferDistance, _defaultBufferParameters);
                Geometry endBuffer2 = endPoint.Buffer(endBufferDistance, _defaultBufferParameters);
                Geometry linearBuffer = lineString!.Buffer(linearBufferDistance, _defaultBufferParameters);

                Geometry union = geometryFactory.CreateGeometryCollection(new[] { endBuffer1, endBuffer2, linearBuffer }).Union();

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
                var window = new ZoneDiffValuesWindow(layerNames);
                Application.ShowModalWindow(window);
                Dictionary<string, double> bufferSizes = window.Zones.ToDictionary(z => z.Layer, z => z.Value);
                //Dictionary<string, double> bufferSizes = new();
                //foreach (string layer in layerNames)
                //{
                //    PromptDoubleOptions pdo = new($"Введите размер буферной зоны для слоя {layer} [Параметры]", "Параметры")
                //    {
                //        AllowNegative = false,
                //        AllowZero = true,
                //        AllowNone = false,
                //        AppendKeywordsToMessage = true
                //    };
                //    PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                //    if (result.Status == PromptStatus.Cancel)
                //        return;
                //    while (result.Status == PromptStatus.Keyword)
                //    {
                //        if (result.StringResult == "Параметры")
                //        {
                //            BufferParametersWindow window = new(ref _defaultBufferParameters);
                //            Application.ShowModalWindow(window);
                //        }
                //        result = Workstation.Editor.GetDouble(pdo);
                //    }
                //    if (result.Status != PromptStatus.OK || result.Value == 0)
                //        continue;
                //    bufferSizes[layer] = result.Value;
                //}

                // Создать геометрию буферных зон и объединить
                var buffers = (from Entity entity in entities
                               where entity is Polyline pl
                               let pl = entity as Polyline
                               let size = bufferSizes[pl.Layer]
                               where size > 0
                               let buffer = pl.ToNtsGeometry(geometryFactory).Buffer(size, _defaultBufferParameters) as Polygon
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
                Polygon?[] polygons = closedPolylines.Select(pl => pl.ToNtsGeometry(geometryFactory) as Polygon).ToArray();
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
