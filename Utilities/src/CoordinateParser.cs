using LoaderCore.Interfaces;
using LoaderCore;
using NanocadUtilities;
using System.Text.RegularExpressions;
using System.Windows;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Utilities
{
    public static class CoordinateParser
    {
        [CommandMethod("ПЛЭКСЕЛЬ")]
        public static void ExcelCoordinatesToPolyline()
        {
            string? clipboardText = Clipboard.GetText();
            string? clipboardModified = clipboardText.Replace(",", ".");
            var matches = Regex.Matches(clipboardModified, @"^|\n(\d*\.\d{1,4})\t(\d*\.\d{1,4})\n|$"); // ищет пары координат, разделённые табуляцией
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Polyline polyline = new()
                {
                    LayerId = Workstation.Database.Clayer
                };
                IEntityFormatter? formatter = NcetCore.ServiceProvider.GetService<IEntityFormatter>();
                formatter?.FormatEntity(polyline);
                foreach (Match match in matches)
                {
                    try
                    {
                        var point = new Point2d(double.Parse(match.Groups[0].Value), double.Parse(match.Groups[1].Value));
                        polyline.AddVertexAt(polyline.NumberOfVertices, point, 0, 0d, 0d);
                    }
                    catch (System.Exception ex)
                    {
                        transaction.Abort();
                        return;
                    }
                }
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                modelspace!.AppendEntity(polyline);
                transaction.Commit();
            }
        }
    }
}
