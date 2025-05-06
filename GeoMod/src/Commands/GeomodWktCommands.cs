//System
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
//Microsoft
using Microsoft.Extensions.Logging;
// Nanocad
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
//Internal
using LoaderCore.NanocadUtilities;
using GeoMod.GeometryConverters;
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using GeoMod.NtsServices;
using NetTopologySuite.Geometries.Utilities;
using LoaderCore.Interfaces;

namespace GeoMod.Commands
{
    public class GeomodWktCommands
    {
        private readonly NtsGeometryServices _geometryServices;
        private readonly IEntityFormatter? _formatter;

        public GeomodWktCommands(INtsGeometryServicesFactory geometryServicesFactory, IEntityFormatter? formatter)
        {
            _geometryServices = geometryServicesFactory.Create();
            _formatter = formatter;
        }
        /// <summary>
        /// Создание WKT текста из выбранных геометрий dwg и помещение его в буфер обмена
        /// </summary>
        public void WktToClipboard()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить геометрию dwg и преобразовать её в nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity?[] entities = psr.Value.GetObjectIds().Select(id => transaction.GetObject(id, OpenMode.ForRead) as Entity).ToArray();
                Geometry?[] geometries = EntityToGeometryConverter.TransferGeometry(entities!, geometryFactory).ToArray();

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

        public void GeometryFromClipboardWkt()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало команды ВКТИМПОРТ", nameof(GeomodWktCommands));
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            Workstation.Logger?.LogDebug("{ProcessingObject}: Получение текста из буфера обмена", nameof(GeomodWktCommands));
            string fromClipboard = System.Windows.Clipboard.GetText();
            Workstation.Logger?.LogDebug("{ProcessingObject}: Текст в буфере обмена:\n{ClipboardText}", nameof(GeomodWktCommands), fromClipboard);


            // отфильтровать текст, описывающий wkt геометрию
            string[] matches = Regex.Matches(fromClipboard, @"[a-zA-Z]+\s?\([^A-Za-zА-Яа-я]*\)").Select(m => m.Value).ToArray();
            WKTReader reader = new(_geometryServices);

            Workstation.Logger?.LogDebug("{ProcessingObject}: Опознано геометрий в формате WKT - {Number}", nameof(GeomodWktCommands), matches.Length);
            // создать геометрию, преобразовать в объекты dwg и поместить в модель
            List<Geometry> geometries = new();
            foreach (var match in matches)
            {
                try
                {
                    Geometry geometry = reader.Read(match);
                    geometries.Add(geometry);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Прочитан объект {GeometryType}", nameof(GeomodWktCommands), geometry.GeometryType);
                }
                catch (System.Exception ex)
                {
                    Workstation.Logger?.LogDebug(ex, "{ProcessingObject}: Некорректная строка WKT: \"{match}\"", nameof(GeomodWktCommands), match);
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
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Объект {GeometryType} конвертирован в полилинии. Число полилиний - {PolylinesNumber}",
                                                nameof(GeomodWktCommands),
                                                geom.GeometryType,
                                                newPolylines.Count());
                }
                if (polylines.Any())
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Добавление {Number} полилиний в чертёж", nameof(GeomodWktCommands), polylines.Count);
                    foreach (Polyline polyline in polylines)
                    {
                        _formatter?.FormatEntity(polyline);
                        modelSpace.AppendEntity(polyline);
                    }
                }
                else
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Полилинии не добавлены", nameof(GeomodWktCommands));
                }
                transaction.Commit();
                Workstation.Logger?.LogInformation("{Number} полилиний добавлено в чертёж", polylines.Count);
            }

        }

        public void WktMultiGeometryToClipboard()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // получить геометрию dwg и преобразовать её в nts
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                Entity[] entities = psr.Value.GetObjectIds().Select(id => id.GetObject<Entity>(OpenMode.ForRead, transaction)).ToArray();

                if (entities.Select(e => e.LayerId).Distinct().Count() > 1)
                    Workstation.Logger?.LogWarning("Внимание. В выборке объекты из разных слоёв! Возможно, непреднамеренный выбор");

                Geometry[] geometries = EntityToGeometryConverter.TransferGeometry(entities, geometryFactory).ToArray();

                // Проверить внутренние кольца
                if (geometries.All(g => g is Polygon))
                {
                    List<Geometry> newPolygons = new();
                    var orderedSet = geometries.OrderByDescending(p => p.Area).ToHashSet();
                    // пройтись по всем полигонам, начиная с самого большого.
                    foreach (var polygon in orderedSet)
                    {
                        // найти все внутренние кольца
                        //var innerRings = orderedSet.Where(g => g != polygon && g.Within(polygon));
                        var innerRings = orderedSet.Where(g => !g.Equals(polygon) && g.Within(polygon)).ToArray();
                        Geometry newPolygon;
                        if (innerRings.Any())
                        {
                            var unionRing = innerRings.Aggregate((p1, p2) => p1.Union(p2));
                            newPolygon = polygon.Difference(unionRing);
                            // исключить все найденные внутренние кольца
                            foreach (var geom in innerRings)
                                orderedSet.Remove(geom);
                        }
                        else
                        {
                            newPolygon = polygon;
                        }
                        // исключить сам полигон - так как в текущей итерации он самый большой, внутренним он быть уже не может
                        orderedSet.Remove(polygon);
                        newPolygons.Add(newPolygon);
                    }
                    geometries = newPolygons.ToArray();
                }

                // создать составную геометрию и исправить наложения
                Geometry newGeometry = GeometryFixer.Fix(geometryFactory.BuildGeometry(geometries), true);

                if (newGeometry is GeometryCollection collection && !collection.IsHomogeneous)
                    Workstation.Logger?.LogWarning("Внимание. Создан объект GeometryCollection. Если предполагались MultiPolygon или MultiLineString - проверьте замкнутость полилиний");

                // создать райтер, преобразовать геометрию в вкт текст, поместить в буфер
                WKTWriter writer = new()
                {
                    OutputOrdinates = Ordinates.XY
                };
                string outputWkt = writer.Write(newGeometry);
                System.Windows.Clipboard.SetText(outputWkt);
                transaction.Commit();
            }
        }

        public void FeatureFromClipboard()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало команды ФИЧИМПОРТ", nameof(GeomodWktCommands));
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            Workstation.Logger?.LogDebug("{ProcessingObject}: Получение текста из буфера обмена", nameof(GeomodWktCommands));
            string fromClipboard = System.Windows.Clipboard.GetText();
            Workstation.Logger?.LogDebug("{ProcessingObject}: Текст в буфере обмена:\n{ClipboardText}", nameof(GeomodWktCommands), fromClipboard);

            Match headerMatch = Regex.Match(fromClipboard, @"^(\w+\t?)+");
            if (!headerMatch.Success)
            {
                Workstation.Logger?.LogWarning("Текст в буфере обмена не содержит заголовков для определения типа объекта");
                return;
            }
            string[] headers = headerMatch.Groups.Values.Select(v => v.Value).ToArray();
            IEnumerable<Dictionary<string, string>> featuresData = fromClipboard.Split("\n")
                                                                                .Skip(1)
                                                                                .Select(str => str.Split("\t").ToDictionary(sub => headers[str.IndexOf(sub)], sub => sub));
        }

    }
}
