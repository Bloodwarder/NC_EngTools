using System;
using System.Linq;
using System.Collections.Generic;


using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NtsBufferOps = NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Buffer;

using Teigha.Runtime;
using Teigha.DatabaseServices;
using HostMgd.EditorInput;

using NanocadUtilities;
using GeoMod.GeometryExtensions;
using System.Text.RegularExpressions;
using GeoMod.UI;
using HostMgd.ApplicationServices;

namespace GeoMod
{
    /// <summary>
    /// Класс, содержащий гео-команды и вспомогательные данные для их функционирования
    /// </summary>
    public class GeoCommands
    {
        private static NtsGeometryServices _geometryServices = null!;
        private static double _defaultBufferDistance { get; set; } = 1d;

        private static BufferParameters DefaultBufferParameters = new()
        {
            EndCapStyle = EndCapStyle.Round,
            JoinStyle = NtsBufferOps.JoinStyle.Round,
            QuadrantSegments = 6,
            SimplifyFactor = 0.02d,
            IsSingleSided = false
        };

        static GeoCommands()
        {
            InitializeNetTopologySuite();
        }


        /// <summary>
        /// Создание WKT текста из выбранных геометрий dwg и помещение его в буфер обмена
        /// </summary>
        [CommandMethod("ВКТЭКСПОРТ", CommandFlags.UsePickSet)]
        public static void WktToClipboard()
        {
            Workstation.Define();
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить геометрию dwg и преобразовать её в nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();

                // создать райтер, преобразовать геометрию в вкт текст, поместить в буфер
                WKTWriter writer = new()
                {
                    OutputOrdinates = Ordinates.XY
                };
                string outputWkt = string.Join("\n", geometries.Select(g => writer.Write(g)).ToArray());
                System.Windows.Clipboard.SetText(outputWkt);
                transaction.Commit();
            }
        }

        [CommandMethod("ВКТИМПОРТ")]
        public static void GeometryFromClipboardWkt()
        {
            Workstation.Define();
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            string fromClipboard = System.Windows.Clipboard.GetText();

            // отфильтровать текст, описывающий wkt геометрию
            string[] matches = Regex.Matches(fromClipboard, @"[a-zA-Z]*\s?\(.*\)").Select(m => m.Value).ToArray();
            WKTReader reader = new(_geometryServices);

            // создать геометрию, преобразовать в объекты dwg и поместить в модель
            Geometry[] geometries = matches.Select(m => reader.Read(m)).ToArray();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                List<Polyline> polylines = new();
                foreach (Geometry geom in geometries)
                {
                    polylines.AddRange(geom.ToDWGPolylines());
                }
                if (polylines.Count > 0)
                {
                    foreach (Polyline polyline in polylines)
                    {
                        modelSpace!.AppendEntity(polyline);
                    }
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Создать буферную зону заданной величины от выбранных объектов
        /// </summary>
        [CommandMethod("ЗОНА", CommandFlags.UsePickSet)]
        public static void SimpleZone()
        {
            Workstation.Define();
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить выбранные объекты и преобразовать в геометрию nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();
                if (geometries.Length == 0)
                    return;

                // получить размер буферной зоны
                PromptDoubleOptions pdo = new("Введите размер буферной зоны [Параметры]", "Параметры")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = _defaultBufferDistance,
                    AppendKeywordsToMessage = true,
                };

                PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                if (result.Status == PromptStatus.Keyword)
                {
                    BufferParametersWindow window = new BufferParametersWindow(ref DefaultBufferParameters);
                    Application.ShowModalWindow(window);
                    pdo.Message = "Введите размер буферной зоны:";
                    result = Workstation.Editor.GetDouble(pdo);
                }
                if (result.Status != PromptStatus.OK)
                    return;
                double bufferDistance = result.Value;
                _defaultBufferDistance = bufferDistance;

                // Создать геометрию буферных зон и объединить
                Geometry[] buffers = geometries.Select(g => g!.Buffer(bufferDistance, DefaultBufferParameters)).ToArray();
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // Поместить в модель
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in union.ToDWGPolylines())
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Создать буферную зону от выбранных объектов с заданием величины для каждого слоя выбранных объектов
        /// </summary>
        [CommandMethod("ЗОНАДИФФ", CommandFlags.UsePickSet)]
        public static void DiverseZone()
        {
            Workstation.Define();
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
                    if (result.Status == PromptStatus.Keyword)
                    {
                        BufferParametersWindow window = new BufferParametersWindow(ref DefaultBufferParameters);
                        Application.ShowModalWindow(window);
                        pdo.Message = $"Введите размер буферной зоны для слоя {layer}:";
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
                               let buffer = pl.ToNTSGeometry(geometryFactory).Buffer(bufferSizes[pl.Layer], DefaultBufferParameters) as Polygon
                               select buffer).ToArray();
                if (buffers.Length == 0)
                    return;
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // Поместить в модель
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in union.ToDWGPolylines())
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        [CommandMethod("ЗОНОБЪЕД", CommandFlags.UsePickSet)]
        public static void ZoneJoin()
        {
            Workstation.Define();
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
                Geometry?[] fixedPolygons = polygons.Select(g => g!.IsValid ? g : NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(g)).ToArray();

                // Удалить исходные полилинии
                foreach (Polyline pl in closedPolylines)
                    pl.Erase();

                // Объединить геометрию, создать из неё полилинии и поместить в модель
                Geometry union = geometryFactory.CreateGeometryCollection(fixedPolygons).Union();

                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in union.ToDWGPolylines())
                {
                    modelSpace!.AppendEntity(pl);
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// Преобразовать коллекцию dwg геометрии в коллекцию nts геометрии
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        private static IEnumerable<Geometry> TransferGeometry(IEnumerable<Entity> entities, GeometryFactory geometryFactory)
        {
            List<Geometry> geometries = new();
            bool warningShow = false;
            foreach (Entity entity in entities)
            {
                if (entity is Polyline pl)
                {
                    if (pl.HasBulges)
                        warningShow = true;
                    geometries.Add(pl.ToNTSGeometry(geometryFactory));
                }
                else if (entity is BlockReference bref)
                {
                    geometries.Add(bref.ToNTSGeometry(geometryFactory));
                }
                else
                {
                    continue;
                }
            }
            if (warningShow)
                Workstation.Editor.WriteMessage("Внимание! Кривые не поддерживаются. Для корректного вывода аппроксимируйте геометрию с дуговыми сегментами");
            return geometries;
        }

        /// <summary>
        /// Преобразовать dwg геометрию в nts геометрию
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        private static Geometry? TransferGeometry(Entity entity, GeometryFactory geometryFactory)
        {
            if (entity is Polyline pl)
            {
                return pl.ToNTSGeometry(geometryFactory);
            }
            else if (entity is BlockReference bref)
            {
                return bref.ToNTSGeometry(geometryFactory);
            }
            else
            {
                return null;
            }
        }
        private static void InitializeNetTopologySuite()
        {
            NtsGeometryServices.Instance = new NtsGeometryServices( // default CoordinateSequenceFactory
                                                        NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                                                        // default precision model
                                                        new PrecisionModel(1000d),
                                                        // default SRID
                                                        -1,
                                                        // Geometry overlay operation function set to use (Legacy or NG)
                                                        GeometryOverlay.NG,
                                                        // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
                                                        new CoordinateEqualityComparer()
                                                       );
            _geometryServices = NtsGeometryServices.Instance;
        }
    }
}
