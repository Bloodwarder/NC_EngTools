using HostMgd.EditorInput;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NanocadUtilities;
using System.Data;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace Utilities
{
    /// <summary>
    /// Конвертер мультилиний в полилинии
    /// </summary>
    public class MultilineConverter
    {
        /// <summary>
        /// Получить мультилинии с чертежа и конвертировать их в полилинии
        /// </summary>
        [CommandMethod("МЛИНВПЛИН", CommandFlags.UsePickSet)]
        public void ConvertMultilineToPolyline()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты и отфильтровать мультилинии
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                List<Mline> mlines = psr.Value.GetObjectIds()
                                             .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForWrite))
                                             .Where(e => e is Mline)
                                             .Select(e => (Mline)e)
                                             .ToList();
                // Создать построить полилинии по вершинам каждой мультилинии и добавить в новую коллекцию
                List<Polyline> polylines = new List<Polyline>();
                IEntityFormatter? formatter = NcetCore.ServiceProvider.GetService<IEntityFormatter>();
                foreach (Mline multiline in mlines)
                {
                    Polyline polyline = new Polyline
                    {
                        Layer = multiline.Layer
                    };
                    for (int i = 0; i < multiline.NumberOfVertices; i++)
                    {
                        Point3d p = multiline.VertexAt(i);
                        polyline.AddVertexAt(i, new Point2d(p.X, p.Y), 0, 0d, 0d);
                    }
                    formatter?.FormatEntity(polyline);
                    polylines.Add(polyline);
                }

                // Добавить в чертёж полилинии и удалить мультилинии
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;

                foreach (Polyline polyline in polylines)
                    modelspace!.AppendEntity(polyline);
                foreach (Mline multiline in mlines)
                    multiline.Erase();
                transaction.Commit();
            }
        }
    }

}
