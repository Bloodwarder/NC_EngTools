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
            //string? clipboardModified = clipboardText.Replace(",", ".");
            var matches = Regex.Matches(clipboardText, @"(\d*[\.,]\d{1,4})\W*(\d*[\.,]\d{1,4})\W*\n?").Cast<Match>(); // ищет пары координат
            if (!matches.Any())
            {
                Workstation.Logger?.LogInformation("Текст в буфере обмена не содержит координатного описания объекта");
                return;
            }
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
                        double x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        double y = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                        Point2d point = new(x, y);
                        polyline.AddVertexAt(polyline.NumberOfVertices, point, 0, 0d, 0d);
                    }
                    catch (FormatException ex)
                    {
                        Workstation.Logger?.LogWarning(ex, "Ошибка интерпретации координат \"{Text}\" Сообщение: \"{Message}\". Пропуск вершины", match.Value, ex.Message);
                        continue;
                    }
                    catch (ArgumentNullException ex)
                    {
                        Workstation.Logger?.LogWarning(ex, "Отстутсвует одна из координат \"{Text}\". Пропуск вершины", match.Value);
                        continue;
                    }
                }
                if (polyline.GetPoint2dAt(0) == polyline.GetPoint2dAt(polyline.NumberOfVertices - 1))
                {
                    polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
                    polyline.Closed = true;
                }
                Workstation.ModelSpace.AppendEntity(polyline);
                transaction.Commit();
            }
        }
    }
}
