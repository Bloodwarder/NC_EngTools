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

namespace GeoMod.Commands
{
    public class GeomodWktCommands
    {
        private readonly NtsGeometryServices _geometryServices;

        public GeomodWktCommands(INtsGeometryServicesFactory geometryServicesFactory)
        {
            _geometryServices = geometryServicesFactory.Create();
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

    }
}
