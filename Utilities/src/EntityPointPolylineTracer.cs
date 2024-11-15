using HostMgd.EditorInput;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace Utilities
{
    public static partial class EntityPointPolylineTracer
    {
        public static void TracePolyline()
        {
            // Объявить переменную для сохранения id создаваемой полилинии
            ObjectId polylineId;
            // Получаем первые 2 объекта и создаём полилинию из двух точек
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Polyline polyline = new()
                {
                    LayerId = Workstation.Database.Clayer
                };
                IEntityFormatter? formatter = NcetCore.ServiceProvider.GetService<IEntityFormatter>();
                formatter?.FormatEntity(polyline);
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        Point2d p = GetEntityBasePoint();
                        polyline.AddVertexAt(polyline.NumberOfVertices, p, 0, 0d, 0d);
                    }
                    catch (CancelledByUserException)
                    {
                        transaction.Abort();
                        return;
                    }
                }
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                modelspace!.AppendEntity(polyline);
                polylineId = polyline.Id;
                transaction.Commit();
            }

            // Получаем от пользователя объекты и добавляем по ним точки в полилинию до завершения команды
            while (true)
            {
                try
                {
                    using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
                    {
                        Polyline polyline = (Polyline)transaction.GetObject(polylineId, OpenMode.ForWrite);
                        Point2d p = GetEntityBasePoint();
                        polyline.AddVertexAt(polyline.NumberOfVertices, p, 0, 0d, 0d);
                        transaction.Commit();
                    }
                }
                catch (CancelledByUserException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Получает характерную точку объекта
        /// </summary>
        /// <returns>Характерная точка объекта (как правило, центр или точка вставки)</returns>
        /// <exception cref="CancelledByUserException">Срабатывает когда пользователь завершает команду</exception>
        private static Point2d GetEntityBasePoint()
        {
            PromptEntityOptions peo = new("Выберите объект");
            peo.AddAllowedClass(typeof(BlockReference), true);
            peo.AddAllowedClass(typeof(Circle), true);
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
                throw new CancelledByUserException();
            Entity entity = (Entity)Workstation.TransactionManager.TopTransaction.GetObject(result.ObjectId, OpenMode.ForRead);
            switch (entity)
            {
                case BlockReference bref:
                    return new Point2d(bref.Position.X, bref.Position.Y);
                case Circle circle:
                    Point3d p = circle.Center;
                    return new Point2d(p.X, p.Y);
                case Polyline polyline:
                    var closest = polyline.GetClosestPointTo(result.PickedPoint, true);
                    var vertexNumber = (int)Math.Round(polyline.GetParameterAtPoint(closest));
                    var point = polyline.GetPoint2dAt(vertexNumber);
                    return point;
                default:
                    throw new NotImplementedException();
            }
        }
    }

}
