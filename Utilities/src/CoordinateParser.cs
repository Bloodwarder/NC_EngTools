using LoaderCore.Interfaces;
using LoaderCore;
using LoaderCore.NanocadUtilities;
using System.Text.RegularExpressions;
using System.Windows;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Utilities
{
    public static class CoordinateParser
    {
        public static void ExcelCoordinatesToPolyline()
        {
            string? clipboardText = Clipboard.GetText();
            string? clipboardModified = clipboardText.Replace(",", ".");
            var matches = Regex.Matches(clipboardModified, @"(\d*\.\d{1,4})\t(\d*\.\d{1,4})"); // ищет пары координат, разделённые табуляцией
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
                        var point = new Point2d(double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
                        polyline.AddVertexAt(polyline.NumberOfVertices, point, 0, 0d, 0d);
                    }
                    catch (System.Exception ex)
                    {
                        Workstation.Logger?.LogWarning(ex, "Ошибка построения полилинии по координатам: {Message}", ex.Message);
                        transaction.Abort();
                        return;
                    }
                }
                if (polyline.GetPoint2dAt(0) == polyline.GetPoint2dAt(polyline.NumberOfVertices - 1))
                {
                    polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
                    polyline.Closed = true;
                }
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                modelspace!.AppendEntity(polyline);
                transaction.Commit();
            }
        }
    }
}
