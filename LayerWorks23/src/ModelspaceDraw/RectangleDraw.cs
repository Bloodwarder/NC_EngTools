//System

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayerWorks.ExternalData;
using LayerWorks.Commands;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки пустого прямоугольника
    /// </summary>
    public class RectangleDraw : AreaDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public RectangleDraw() { }

        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate.Width, LegendGrid.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate.Height, LegendGrid.CellHeight);
        /// <inheritdoc/>
        public override void Draw()
        {
            DrawRectangle(RectangleWidth, RectangleHeight);
        }

        private protected Polyline DrawRectangle(double width, double height, string layer = null, double brightnessshift = 0d)
        {
            Polyline rectangle = new Polyline();
            rectangle.AddVertexAt(0, GetRelativePoint(-width / 2, -height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(1, GetRelativePoint(-width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, GetRelativePoint(width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(3, GetRelativePoint(width / 2, -height / 2), 0, 0d, 0d);
            rectangle.Closed = true;
            if (layer != null)
                LayerChecker.Check($"{LayerParser.StandartPrefix}_{layer}"); //ПОКА ЗАВЯЗАНО НА ЧЕКЕР ИЗ ДРУГОГО МОДУЛЯ. ПРОАНАЛИЗИРОВАТЬ ВОЗМОЖНОСТИ ОПТИМИЗАЦИИ
            rectangle.Layer = layer == null ? Layer.BoundLayer.Name : $"{LayerParser.StandartPrefix}_{layer}";
            LayerProps lp = LayerPropertiesDictionary.GetValue(rectangle.Layer, out bool success);
            if (success)
            {
                rectangle.LinetypeScale = lp.LTScale;
                rectangle.ConstantWidth = lp.ConstantWidth;
            }
            rectangle.Color = BrightnessShift(brightnessshift);
            EntitiesList.Add(rectangle);
            return rectangle;
        }
    }
}
