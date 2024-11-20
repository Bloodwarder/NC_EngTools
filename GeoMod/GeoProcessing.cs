//System
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
//Microsoft
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// Nanocad
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
//Internal
using LoaderCore;
using LoaderCore.NanocadUtilities;
using GeoMod.GeometryConverters;
using GeoMod.UI;
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Precision;
using NetTopologySuite.Geometries.Utilities;

using NtsBufferOps = NetTopologySuite.Operation.Buffer;

namespace GeoMod
{
    // TODO: ����������������� ���� � ���������

    /// <summary>
    /// �����, ���������� ���-������� � ��������������� ������ ��� �� ����������������
    /// </summary>
    public class GeoProcessing
    {
        private const string RelatedConfigurationSection = "GeoModConfiguration";

        private static NtsGeometryServices _geometryServices = null!;
        private static readonly Microsoft.Extensions.Configuration.IConfigurationSection _configuration;
        private static GeometryPrecisionReducer _reducer = null!;
        private static BufferParameters _defaultBufferParameters;


        static GeoProcessing()
        {
            var section = NcetCore.ServiceProvider.GetRequiredService<IConfiguration>();
            _configuration = section.GetRequiredSection(RelatedConfigurationSection);
            InitializeNetTopologySuite();
            _defaultBufferParameters = _configuration.GetValue<BufferParameters>("BufferParameters") ?? new()
            {
                EndCapStyle = EndCapStyle.Round,
                JoinStyle = NtsBufferOps.JoinStyle.Round,
                QuadrantSegments = 6,
                SimplifyFactor = 0.02d,
                IsSingleSided = false
            };
        }

        private static double DefaultBufferDistance { get; set; } = 1d;

        /// <summary>
        /// �������� WKT ������ �� ��������� ��������� dwg � ��������� ��� � ����� ������
        /// </summary>
        public static void WktToClipboard()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� dwg � ������������� � � nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();

                // ������� ������, ������������� ��������� � ��� �����, ��������� � �����
                WKTWriter writer = new()
                {
                    OutputOrdinates = Ordinates.XY
                };
                string outputWkt = string.Join("\n", geometries.Select(g => writer.Write(g)).ToArray());
                System.Windows.Clipboard.SetText(outputWkt);
                transaction.Commit();
            }
        }

        public static void GeometryFromClipboardWkt()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: ������ ������� ���������", nameof(GeoProcessing));
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            Workstation.Logger?.LogDebug("{ProcessingObject}: ��������� ������ �� ������ ������", nameof(GeoProcessing));
            string fromClipboard = System.Windows.Clipboard.GetText();
            Workstation.Logger?.LogDebug("{ProcessingObject}: ����� � ������ ������:\n{ClipboardText}", nameof(GeoProcessing), fromClipboard);


            // ������������� �����, ����������� wkt ���������
            string[] matches = Regex.Matches(fromClipboard, @"[a-zA-Z]+\s?\([^A-Za-z�-��-�]*\)").Select(m => m.Value).ToArray();
            WKTReader reader = new(_geometryServices);

            Workstation.Logger?.LogDebug("{ProcessingObject}: �������� ��������� � ������� WKT - {Number}", nameof(GeoProcessing), matches.Length);
            // ������� ���������, ������������� � ������� dwg � ��������� � ������
            List<Geometry> geometries = new();
            foreach(var match in matches)
            {
                try
                {
                    Geometry geometry = reader.Read(match);
                    geometries.Add(geometry);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: �������� ������ {GeometryType}", nameof(GeoProcessing), geometry.GeometryType);
                }
                catch (System.Exception ex)
                {
                    Workstation.Logger?.LogDebug(ex, "{ProcessingObject}: ������������ ������ WKT: \"{match}\"", nameof(GeoProcessing), match);
                    continue;
                }
            }
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = Workstation.ModelSpace;

                List<Polyline> polylines = new();
                foreach (Geometry geom in geometries)
                {
                    var newPolylines = GeometryToDwgConverter.ToDWGPolylines(geom);
                    polylines.AddRange(newPolylines);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: ������ {GeometryType} ������������� � ���������. ����� ��������� - {PolylinesNumber}",
                                                nameof(GeoProcessing),
                                                geom.GeometryType,
                                                newPolylines.Count());
                }
                if (polylines.Any())
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: ���������� {Number} ��������� � �����", nameof(GeoProcessing), polylines.Count);
                    foreach (Polyline polyline in polylines)
                    {
                        modelSpace.AppendEntity(polyline);
                    }
                }
                else
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: ��������� �� ���������", nameof(GeoProcessing));
                }
                transaction.Commit();
                Workstation.Logger?.LogInformation("{Number} ��������� ��������� � �����", polylines.Count);
            }
        }

        /// <summary>
        /// ������� �������� ���� �������� �������� �� ��������� ��������
        /// </summary>
        public static void SimpleZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� ������� � ������������� � ��������� nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = TransferGeometry(entities!, geometryFactory).ToArray();
                if (geometries.Length == 0)
                    return;

                // �������� ������ �������� ����
                PromptDoubleOptions pdo = new("������� ������ �������� ���� [���������]", "���������")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    DefaultValue = DefaultBufferDistance,
                    AppendKeywordsToMessage = true,
                };

                PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                if (result.Status == PromptStatus.Keyword)
                {
                    BufferParametersWindow window = new(ref _defaultBufferParameters);
                    Application.ShowModalWindow(window);
                    pdo.Message = "������� ������ �������� ����:";
                    result = Workstation.Editor.GetDouble(pdo);
                }
                if (result.Status != PromptStatus.OK)
                    return;
                double bufferDistance = result.Value;
                DefaultBufferDistance = bufferDistance;

                // ������� ��������� �������� ��� � ����������
                Geometry[] buffers = geometries.Select(g => g!.Buffer(bufferDistance, _defaultBufferParameters)).ToArray();
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // ��������� � ������
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in GeometryToDwgConverter.ToDWGPolylines(union))
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// ������� �������� ���� �� ��������� �������� � �������� �������� ��� ������� ���� ��������� ��������
        /// </summary>
        public static void DiverseZone()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� �������
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();

                // �������� ���� ��������� ��������
                string[] layerNames = entities.Select(e => e!.Layer).Distinct().ToArray();

                // �������� ���� ������������ - ������ �������� ���� ��� ������� ����, � ��������� � �������
                Dictionary<string, double> bufferSizes = new();
                foreach (string layer in layerNames)
                {
                    PromptDoubleOptions pdo = new($"������� ������ �������� ���� ��� ���� {layer} [���������]", "���������")
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
                        BufferParametersWindow window = new(ref _defaultBufferParameters);
                        Application.ShowModalWindow(window);
                        pdo.Message = $"������� ������ �������� ���� ��� ���� {layer}:";
                        result = Workstation.Editor.GetDouble(pdo);
                    }
                    if (result.Status != PromptStatus.OK || result.Value == 0)
                        continue;
                    bufferSizes[layer] = result.Value;
                }

                // ������� ��������� �������� ��� � ����������
                var buffers = (from Entity entity in entities
                               where entity is Polyline
                               let pl = entity as Polyline
                               let buffer = pl.ToNTSGeometry(geometryFactory).Buffer(bufferSizes[pl.Layer], _defaultBufferParameters) as Polygon
                               select buffer).ToArray();
                if (buffers.Length == 0)
                    return;
                Geometry union = geometryFactory.CreateGeometryCollection(buffers).Union();

                // ��������� � ������
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Polyline pl in GeometryToDwgConverter.ToDWGPolylines(union))
                {
                    modelSpace!.AppendEntity(pl);
                }
                transaction.Commit();
            }
        }

        public static void ZoneJoin()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� �������, ������������� ��������� ��������� � ������� �� ��� nts ��������
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var closedPolylines = (from ObjectId id in entitiesIds
                                       let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                       where entity is Polyline pl && pl.Closed
                                       select entity as Polyline).ToArray();
                // ������� �������������� � �������������� ��������
                if (closedPolylines.Length == 0)
                {
                    Workstation.Editor.WriteMessage("��� ���������� ��������� ��������� � ������");
                    return;
                }
                if (closedPolylines.Length < entitiesIds.Length)
                    Workstation.Editor.WriteMessage($"�� ���������� {entitiesIds.Length - closedPolylines.Length} ��������, �� ���������� ���������� �����������");

                // �������� ��������� ���������
                Polygon?[] polygons = closedPolylines.Select(pl => pl.ToNTSGeometry(geometryFactory) as Polygon).ToArray();
                Geometry?[] fixedPolygons = polygons.Select(g => g!.IsValid ? g : GeometryFixer.Fix(g)).ToArray();

                // ������� �������� ���������
                foreach (Polyline pl in closedPolylines)
                    pl.Erase();

                // ���������� ���������, ������� �� �� ��������� � ��������� � ������
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

        /// <summary>
        /// ������������� ��������� dwg ��������� � ��������� nts ���������
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
                Workstation.Editor.WriteMessage("��������! ������ �� ��������������. ��� ����������� ������ ��������������� ��������� � �������� ����������");
            return geometries;
        }

        /// <summary>
        /// ������������� dwg ��������� � nts ���������
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

        // TODO: ����������� ���������� ����� ��� ��������� ���������� ������ ���������
        public static void ReduceCoordinatePrecision()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� �������, ������������� ��������� � ������� �� ��� nts ��������
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var polylines = (from ObjectId id in entitiesIds
                                 let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                 where entity is Polyline pl
                                 select entity as Polyline).ToArray();
                // ������� �������������� � �������������� ��������
                if (polylines.Length == 0)
                {
                    Workstation.Editor.WriteMessage("��� ��������� � ������");
                    return;
                }
                if (polylines.Length < entitiesIds.Length)
                    Workstation.Editor.WriteMessage($"�� ���������� {entitiesIds.Length - polylines.Length} ��������, �� ���������� �����������");

                // �������� ��������� ��������� � ��������� �������� ���������, ��� ���� �������� ����� � ��������� �����������
                Dictionary<Geometry, Polyline>  geometries = polylines.Select(p => (p, p.ToNTSGeometry(geometryFactory)))
                                                                      .Select(t => (t.p, t.Item2.IsValid ? t.Item2 : GeometryFixer.Fix(t.Item2)))
                                                                      .Select(t => (t.p, _reducer.Reduce(t.Item2)))
                                                                      .ToDictionary(t => t.Item2, t => t.p);

                // ���������� ���������, ������� �� �� ��������� � ��������� � ������
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // �������� ��� ���������, �������� �������� �������� ���������
                geometries.Keys.SelectMany(g => GeometryToDwgConverter.ToDWGPolylines(g).Select(p => CopySourceProperties(p, geometries[g])))
                               .ToList()
                               .ForEach(pl => modelSpace!.AppendEntity(pl));

                // ������� �������� ���������
                foreach (Polyline pl in polylines)
                    pl.Erase();
                
                transaction.Commit();
            }
        }

        private static Polyline CopySourceProperties(Polyline p, Polyline source)
        {
            p.Layer = source.Layer;
            p.ConstantWidth = source.ConstantWidth;
            p.LinetypeScale = source.LinetypeScale;
            p.LineWeight = source.LineWeight;
            p.Color = source.Color;
            return p;
        }

        private static void InitializeNetTopologySuite()
        {
            double precision = _configuration.GetValue<double>("Precision");
            NtsGeometryServices.Instance = new NtsGeometryServices( // default CoordinateSequenceFactory
                                                        NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                                                        // default precision model
                                                        new PrecisionModel(precision),
                                                        // default SRID
                                                        -1,
                                                        // Geometry overlay operation function set to use (Legacy or NG)
                                                        GeometryOverlay.NG,
                                                        // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
                                                        new CoordinateEqualityComparer()
                                                       );
            _geometryServices = NtsGeometryServices.Instance;

            double reducedPrecision = _configuration.GetValue<double>("ReducedPrecision");
            PrecisionModel precisionModel = new(reducedPrecision);
            _reducer = new GeometryPrecisionReducer(precisionModel);
        }
    }
}
