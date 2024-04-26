//System

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Colors;

using LayerWorks.LayerProcessing;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки двойной линии (одна над другой, верхняя линия по стандарту слоя)
    /// </summary>
    public class DoubleSolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public DoubleSolidLineDraw() { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer = null) : base(basepoint, layer) { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.LayerInfo.Name;
            Polyline pl2 = pl.Clone() as Polyline;
            bool success  = LayerPropertiesDictionary.TryGetValue(Layer.LayerInfo.TrueName, out LayerProps lp);
            if (success)
            {
                pl.LinetypeScale = lp.LTScale;
                pl.ConstantWidth = lp.ConstantWidth;
            }
            pl2.ConstantWidth = double.Parse(LegendDrawTemplate.Width);  // ТОЖЕ КОСТЫЛЬ, ЧТОБЫ НЕ ДОБАВЛЯТЬ ДОП ПОЛЕ В ТАБЛИЦУ. ТАКИХ СЛОЯ ВСЕГО 3
            pl2.Color = Color.FromRgb(0, 0, 255);   // ЗАТЫЧКА, ПОКА ТАКОЙ ОБЪЕКТ ВСЕГО ОДИН
            EntitiesList.Add(pl2);
            EntitiesList.Add(pl);
        }
    }
}
