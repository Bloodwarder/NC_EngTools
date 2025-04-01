//System

//Modules
//nanoCAD
using LayerWorks.EntityFormatters;
using LayerWorks.LayerProcessing;
using LoaderCore;
using Microsoft.Extensions.DependencyInjection;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки объекта чертежа
    /// </summary>
    public abstract class ObjectDraw
    {
        internal protected static LayerChecker _checker = NcetCore.ServiceProvider.GetRequiredService<LayerChecker>();

        /// <summary>
        /// Список созданных объектов чертежа для вставки в модель целевого чертежа. Заполняется через метод Draw
        /// </summary>
        public List<Entity> EntitiesList { get; } = new List<Entity>();
        /// <summary>
        /// Базовая точка для вставки объектов в целевой чертёж
        /// </summary>
        public Point2d Basepoint { get; set; }
        internal static Color s_byLayer = Color.FromColorIndex(ColorMethod.ByLayer, 256);
        internal ObjectId LayerId { get; set; }

        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public ObjectDraw(Point2d basepoint, RecordLayerWrapper layer)
        {
            Basepoint = basepoint;
            LayerId = layer.BoundLayer.Id;
        }

        public ObjectDraw(Point2d basepoint, ObjectId layerId)
        {
            Basepoint = basepoint;
            LayerId = layerId;
        }
        /// <summary>
        /// Создать объекты чертежа для отрисовки (последующей вставки в модель целевого чертежа). После выполнения доступны через свойство EntitiesList.
        /// </summary>
        public abstract void Draw();
        /// <summary>
        /// Преобразование относительных координат в сетке условных в абсолютные координаты целевого чертежа
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected Point2d GetRelativePoint(double x, double y)
        {
            return new Point2d(x + Basepoint.X, y + Basepoint.Y);
        }
    }
}
