using System;
using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;


using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NtsBufferOps = NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Buffer;

using Teigha.Runtime;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using HostMgd.EditorInput;

using Loader.CoreUtilities;
using System.Text.RegularExpressions;

namespace GeoMod
{
    public class GeoCommands
    {
        internal static NtsGeometryServices GeometryServices;
        internal static double DefaultBufferDistance { get; set; } = 1d;

        private static BufferParameters DefaultBufferParameters { get; } = new BufferParameters()
        {
            EndCapStyle = EndCapStyle.Round,
            JoinStyle = NtsBufferOps.JoinStyle.Round,
            QuadrantSegments = 6,
            SimplifyFactor = 0.02d,
            IsSingleSided = false
        };

        static GeoCommands()
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
            GeometryServices = NtsGeometryServices.Instance;
        }

        [CommandMethod("ВКТТЕКСТ", CommandFlags.UsePickSet)]
        public static void WktToClipboard()
        {
            Workstation.Define();
            var geometryFactory = GeometryServices.CreateGeometryFactory();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();

                WKTWriter writer = new()
                {
                    OutputOrdinates = Ordinates.XY
                };
                string outputWkt = string.Join("\n", geometries.Select(g => writer.Write(g)).ToArray());
                System.Windows.Forms.Clipboard.SetText(outputWkt);
                transaction.Commit();
            }
        }

        [CommandMethod("ИЗВКТ")]
        public static void GeometryFromClipboardWkt()
        {
            Workstation.Define();
            var geometryFactory = GeometryServices.CreateGeometryFactory();
            string fromClipboard = System.Windows.Forms.Clipboard.GetText();
            string[] matches = Regex.Matches(fromClipboard, @"[a-zA-Z]*\s?\(.*\)").Select(m => m.Value).ToArray();
            WKTReader reader = new(GeometryServices);
            Geometry[] geometries = matches.Select(m => reader.Read(m)).ToArray();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                List<Polyline> polylines = new List<Polyline>();
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

        [CommandMethod("ЗОНА", CommandFlags.UsePickSet)]
        public static void SimpleZone()
        {
            Workstation.Define();
            var geometryFactory = GeometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();
                if (geometries.Length == 0)
                    return;

                PromptDoubleOptions pdo = new("Введите размер буферной зоны")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = DefaultBufferDistance
                };
                PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                if (result.Status != PromptStatus.OK)
                    return;
                double bufferDistance = result.Value;
                DefaultBufferDistance = bufferDistance;
                Geometry[] buffers = geometries.Select(g => g!.Buffer(bufferDistance, DefaultBufferParameters)).ToArray();

                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();
                foreach (Polyline pl in union.ToDWGPolylines())
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        [CommandMethod("ЗОНАДИФФ", CommandFlags.UsePickSet)]
        public static void DiverseZone()
        {
            Workstation.Define();
            var geometryFactory = GeometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();

                string[] layerNames = entities.Select(e => e!.Layer).Distinct().ToArray();
                Dictionary<string, double> bufferSizes = new();
                foreach (string layer in layerNames)
                {
                    PromptDoubleOptions pdo = new($"Введите размер буферной зоны для слоя {layer}")
                    {
                        AllowNegative = false,
                        AllowZero = true,
                        AllowNone = false,
                    };
                    PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                    if (result.Status == PromptStatus.Cancel)
                        return;
                    if (result.Status != PromptStatus.OK || result.Value == 0)
                        continue;
                    bufferSizes[layer] = result.Value;
                }

                var buffers = (from Entity entity in entities
                               where entity is Polyline
                               let pl = entity as Polyline
                               let buffer = pl.ToNTSGeometry(geometryFactory).Buffer(bufferSizes[pl.Layer], DefaultBufferParameters) as Polygon
                               select buffer).ToArray();
                if (buffers.Length == 0)
                    return;
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

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
            var geometryFactory = GeometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var closedPolylines = (from ObjectId id in entitiesIds
                                       let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                       where entity is Polyline pl && pl.Closed
                                       select entity as Polyline).ToArray();
                if (closedPolylines.Length == 0)
                {
                    Workstation.Editor.WriteMessage("Нет подходящих замкнутых полилиний в наборе");
                    return;
                }

                if (closedPolylines.Length < entitiesIds.Length)
                    Workstation.Editor.WriteMessage($"Не обработано {entitiesIds.Length - closedPolylines.Length} объектов, не являющихся замкнутыми полилиниями");

                Polygon?[] polygons = closedPolylines.Select(pl => pl.ToNTSGeometry(geometryFactory) as Polygon).ToArray();
                Geometry?[] fixedPolygons = polygons.Select(g => g!.IsValid ? g : NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(g)).ToArray();

                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in closedPolylines)
                    pl.Erase();
                Geometry union = geometryFactory.CreateGeometryCollection(fixedPolygons).Union();
                foreach (Polyline pl in union.ToDWGPolylines())
                {
                    modelSpace!.AppendEntity(pl);
                }

                transaction.Commit();
            }
        }

        internal static IEnumerable<Geometry> TransferGeometry(IEnumerable<Entity> entities, GeometryFactory geometryFactory)
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

        internal static Geometry? TransferGeometry(Entity entity, GeometryFactory geometryFactory)
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
    }

    // Методы расширения для геометрий (и DWG, и NetTopology)

    public static class PolylineExtension
    {
        public static Geometry ToNTSGeometry(this Polyline polyline, GeometryFactory geometryFactory)
        {
            Coordinate[] coordinates = new Coordinate[polyline.NumberOfVertices];
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point2d p = polyline.GetPoint2dAt(i);
                coordinates[i] = new(p.X, p.Y);
            }
            if (polyline.Closed)
            {
                if (coordinates[0].Equals(coordinates[coordinates.Length - 1]))
                {
                    return geometryFactory.CreatePolygon(coordinates);
                }
                else
                {
                    List<Coordinate> coordsClosed = new(coordinates);
                    coordsClosed.Add(coordsClosed[0].Copy());
                    return geometryFactory.CreatePolygon(coordsClosed.ToArray());
                }
            }
            else
            {
                return geometryFactory.CreateLineString(coordinates);
            }
        }
    }

    public static class BlockReferenceExtension
    {
        public static Point ToNTSGeometry(this BlockReference blockRef, GeometryFactory geometryFactory)
        {
            Point point = geometryFactory.CreatePoint(new Coordinate(blockRef.Position.X, blockRef.Position.Y));
            return point;
        }
    }

    public static class LineStringExtension
    {
        public static Polyline ToDWGPolyline(this LineString lineString)
        {
            return ProcessGeometry(lineString);
        }

        internal static Polyline ProcessGeometry(Geometry geometry)
        {
            Polyline polyline = new()
            {
                LayerId = Workstation.Database.Clayer
            };
            Coordinate[] coordinates = geometry.Coordinates;
            for (int i = 0; i < coordinates.Length; i++)
            {
                polyline.AddVertexAt(i, new Point2d(coordinates[i].X, coordinates[i].Y), 0, 0, 0);
            }
            return polyline;
        }

    }

    public static class ClosedGeometryExtension
    {
        public static Polyline ToDWGPolyline(this LinearRing linearRing)
        {
            Polyline polyline = LineStringExtension.ProcessGeometry(linearRing);
            polyline.Closed = true;
            return polyline;
        }

        public static IEnumerable<Polyline> ToDWGPolylines(this Polygon polygon)
        {
            List<Polyline> polylines = new();
            LinearRing? exteriorRing = polygon.ExteriorRing as LinearRing;
            polylines.Add(exteriorRing!.ToDWGPolyline());
            foreach (LinearRing geom in polygon.InteriorRings.Cast<LinearRing>())
            {
                polylines.Add(geom.ToDWGPolyline());
            }
            return polylines;
        }

        public static IEnumerable<Polyline> ToDWGPolylines(this MultiPolygon mPolygon)
        {
            List<Polyline> polylines = new();

            foreach (Polygon? polygon in mPolygon.Geometries.Select(g => g as Polygon))
            {
                polylines.AddRange(polygon!.ToDWGPolylines());
            }
            return polylines;
        }
        public static IEnumerable<Polyline> ToDWGPolylines(this MultiLineString mLinestring)
        {
            List<Polyline> polylines = new();

            foreach (LineString? linestring in mLinestring.Geometries.Select(g => g as LineString))
            {
                polylines.AddRange(linestring!.ToDWGPolylines());
            }
            return polylines;
        }

        public static IEnumerable<Polyline> ToDWGPolylines(this Geometry geometry)
        {
            Type type = geometry.GetType();
            List<Polyline> polylines = new();

            switch (type.Name.ToUpper())
            {
                case "LINESTRING":
                    LineString? ls = geometry as LineString;
                    polylines.Add(ls!.ToDWGPolyline());
                    break;
                case "LINEARRING":
                    LinearRing? lr = geometry as LinearRing;
                    polylines.Add(lr!.ToDWGPolyline());
                    break;
                case "POLYGON":
                    Polygon? pg = geometry! as Polygon;
                    polylines.AddRange(pg!.ToDWGPolylines());
                    break;
                case "MULTIPOLYGON":
                    MultiPolygon? mpg = geometry! as MultiPolygon;
                    polylines.AddRange(mpg!.ToDWGPolylines());
                    break;
                case "MULTILINESTRING":
                    MultiLineString? mls = geometry! as MultiLineString;
                    polylines.AddRange(mls!.ToDWGPolylines());
                    break;
                default:
                    break;
            }
            return polylines;
        }
    }
}
