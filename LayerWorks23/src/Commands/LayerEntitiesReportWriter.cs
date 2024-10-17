using LayersIO.Excel;
using LayerWorks.LayerProcessing;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
using System.IO;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Teigha.Colors;

namespace LayerWorks.Commands
{
    public static class LayerEntitiesReportWriter
    {
        private static Color _byLayerColor = Color.FromColorIndex(ColorMethod.ByLayer, 256);

        public static void WriteReport()
        {
            // TODO: Пока сделано "в лоб", чтобы работало. Много вариантов для оптимизации. Вынести логику фильтрации в xml
            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable blocktable = (BlockTable)transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false);
                BlockTableRecord modelspace = (BlockTableRecord)transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                var parser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
                var entities = modelspace.Cast<ObjectId>()
                                         .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                                         .Where(e => e.Layer.StartsWith(parser.Prefix + parser.Separator))
                                         .Where(e => e is Polyline);
                List<EntityLayerWrapper> wrappers = new();
                HashSet<string> filteredStatus = new() { "пр", "дем" };

                foreach (var entity in entities)
                {
                    try
                    {
                        var wrapper = new EntityLayerWrapper(entity);
                        if (wrapper.LayerInfo.IsValid
                            && filteredStatus.Contains(wrapper.LayerInfo.Status)
                            && wrapper.BoundEntities.All(e => e.Color == _byLayerColor))
                            wrappers.Add(new(entity));
                    }
                    catch
                    {
                        continue;
                    }
                }
                var reports = wrappers.GroupBy(w => new { w.LayerInfo.MainName, w.LayerInfo.Status, Reconstruction = w.LayerInfo.SuffixTagged["Reconstruction"] })
                                          .Select(group => new LayerEntityReport()
                                          {
                                              MainName = group.Key.MainName,
                                              Status = group.Key.Status,
                                              Reconstruction = (group.Key.Reconstruction == true || group.Key.Status == "дем") ? "Переустройство" : "Новое строительство",
                                              CumulativeLength = group.SelectMany(w => w.BoundEntities)
                                                                      .Select(e => (Polyline)e)
                                                                      .Sum(pl => Math.Round(pl.Length / 1000, 3))
                                          })
                                          .OrderBy(r => r.MainName)
                                          .ThenBy(r => r.Reconstruction)
                                          .ThenBy(r => r.Status)
                                          .ToArray();
                string dwgPath = Workstation.Database.Filename;
                var dir = new FileInfo(dwgPath).Directory;
                var reportPath = Path.Combine(dir!.FullName, $"LayerReport_{DateTime.Now.ToLongTimeString().Replace(":", "-")}.xlsx");
                var writer = new ExcelSimpleReportWriter<LayerEntityReport>(reportPath, "Report");
                writer.ExportToExcel(reports.ToArray());

            }
        }

        class LayerEntityReport
        {
            public string MainName { get; set; }
            public string Status { get; set; }
            public string Reconstruction { get; set; }
            public string Units { get; set; } = "км";
            public double CumulativeLength { get; set; }
        }

    }
}
