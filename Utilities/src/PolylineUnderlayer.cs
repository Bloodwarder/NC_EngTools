using HostMgd.EditorInput;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Utilities
{
    public class PolylineUnderlayer
    {
        public static void CreateUnderlayingPolyline()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало пользовательского ввода", nameof(PolylineUnderlayer));

            PromptEntityOptions peo = new("Выберите полилинию для создания подложки")
            {
                AllowNone = false,
            };
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult entityResult = Workstation.Editor.GetEntity(peo);
            if (entityResult.Status != PromptStatus.OK)
            {
                Workstation.Logger?.LogError("Ошибка выбора полилинии");
                return;
            }
            ObjectId polylineId = entityResult.ObjectId;

            PromptPointOptions ppo = new("Выберите точку начала подложки")
            {
                UseBasePoint = false,
                AllowNone = false,
            };
            PromptPointResult pointResult1 = Workstation.Editor.GetPoint(ppo);
            if (entityResult.Status != PromptStatus.OK)
            {
                Workstation.Logger?.LogError("Ошибка выбора точки начала");
                return;
            }
            Point3d p1 = pointResult1.Value;
            Workstation.Logger?.LogDebug("{ProcessingObject}: Выбрана начальная точка - {Point1}",
                                         nameof(PolylineUnderlayer),
                                         p1.ToString(CultureInfo.InvariantCulture));

            PromptPointOptions ppo2 = new("Выберите точку окончания подложки")
            {
                UseBasePoint = true,
                UseDashedLine = true,
                AllowNone = false,
                BasePoint = p1
            };
            PromptPointResult pointResult2 = Workstation.Editor.GetPoint(ppo2);
            if (entityResult.Status != PromptStatus.OK)
            {
                Workstation.Logger?.LogError("Ошибка выбора точки окончания");
                return;
            }
            Point3d p2 = pointResult2.Value;
            Workstation.Logger?.LogDebug("{ProcessingObject}: Выбрана конечная точка - {Point1}",
                                         nameof(PolylineUnderlayer),
                                         p1.ToString(CultureInfo.InvariantCulture));

            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                Polyline polyline = (Polyline)transaction.GetObject(polylineId, OpenMode.ForRead);
                var closestP1 = polyline.GetClosestPointTo(p1, false);
                var closestP2 = polyline.GetClosestPointTo(p2, false);
                double parameter1 = polyline.GetParameterAtPoint(closestP1);
                double parameter2 = polyline.GetParameterAtPoint(closestP2);
                if (parameter1 > parameter2)
                {
                    (closestP1, closestP2) = (closestP2, closestP1);
                    (parameter1, parameter2) = (parameter2, parameter1);
                }
                Polyline newPolyline = new();
                newPolyline.AddVertexAt(0, closestP1.Convert2d(new Plane()), 0d, 0d, 0d);
                int pointsBetween = (int)Math.Floor(parameter2) - (int)Math.Floor(parameter1);
                for (int i = 0; i < pointsBetween; i++)
                {
                    Point2d newPoint = polyline.GetPoint2dAt((int)Math.Floor(parameter1) + i + 1);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Промежуточная точка - {Point1}",
                                                 nameof(PolylineUnderlayer),
                                                 newPoint.ToString(CultureInfo.InvariantCulture));
                    newPolyline.AddVertexAt(i + 1, newPoint, 0, 0, 0);
                }
                newPolyline.AddVertexAt(pointsBetween + 1, closestP2.Convert2d(new Plane()), 0d, 0d, 0d);
                newPolyline.LayerId = Workstation.Database.Clayer;

                Workstation.Logger?.LogDebug("{ProcessingObject}: Добавление полилинии в модель", nameof(PolylineUnderlayer));

                Workstation.ModelSpace.AppendEntity(newPolyline);
                transaction.AddNewlyCreatedDBObject(newPolyline, true);

                Workstation.Logger?.LogDebug("{ProcessingObject}: Форматирование полилинии", nameof(PolylineUnderlayer));

                IEntityFormatter? formatter = NcetCore.ServiceProvider.GetService<IEntityFormatter>();
                formatter?.FormatEntity(newPolyline);

                Workstation.Logger?.LogDebug("{ProcessingObject}: Установка порядка прорисовки", nameof(PolylineUnderlayer));

                DrawOrderTable dot = (DrawOrderTable)transaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
                dot.MoveBelow(new() { newPolyline.Id }, polylineId);

                Workstation.Logger?.LogDebug("{ProcessingObject}: Завершение транзакции", nameof(PolylineUnderlayer));
                transaction.Commit();
            }
        }
    }
}
