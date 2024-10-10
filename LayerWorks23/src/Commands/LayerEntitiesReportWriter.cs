using HostMgd.EditorInput;
using HostMgd.Runtime;
using LayersIO.Excel;
using LayerWorks.LayerProcessing;
using NameClassifiers;
using NanocadUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace LayerWorks.Commands
{
    public static class LayerEntitiesReportWriter
    {


        [CommandMethod("СЛОЙОТЧЕТ")]
        public static void WriteReport()
        {
            Workstation.Define();

            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable blocktable = (BlockTable)transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false);
                BlockTableRecord modelspace = (BlockTableRecord)transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                var parser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
                var entities = modelspace.Cast<ObjectId>()
                                         .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                                         .Where(e => e.Layer.StartsWith(parser.Prefix + parser.Separator));
                List<EntityLayerWrapper> wrappers = new();
                foreach (var entity in entities)
                {
                    try
                    {
                        wrappers.Add(new(entity));
                    }
                    catch
                    {
                        continue;
                    }
                }
                HashSet<string> filteredStatus = new() { "пр", "дем" };
                var filteredWrappers = wrappers.Where(w => filteredStatus.Contains(w.LayerInfo.Status) && w.BoundEntities.All(e => e is Polyline));
                var foo = filteredWrappers.GroupBy(w => new { w.LayerInfo.MainName, w.LayerInfo.Status })
                                          .Select(group => new LayerEntityReport()
                                          {
                                              MainName = group.Key.MainName,
                                              Status = group.Key.Status,
                                              CumulativeLength = group.SelectMany(w => w.BoundEntities).Select(e => (Polyline)e).Sum(pl => pl.Length)
                                          })
                                          .OrderBy(f => f.MainName)
                                          .ThenBy(f => f.Status)
                                          .ToArray();
                string dwgPath = Workstation.Database.Filename;
                var dir = new FileInfo(dwgPath).Directory;
                var reportPath = Path.Combine(dir!.FullName, $"LayerReport_{DateTime.Now.ToShortTimeString().Replace(":","_")}.xlsx");
                var writer = new ExcelSimpleReportWriter<LayerEntityReport>(reportPath, "Report");
                writer.ExportToExcel(foo.ToArray());

            }
        }

        class LayerEntityReport
        {
            public string MainName { get; set; }
            public string Status { get; set; }
            public double CumulativeLength { get; set; }
        }

    }
}
