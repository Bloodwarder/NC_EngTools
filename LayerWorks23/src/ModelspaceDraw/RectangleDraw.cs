//System

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using NameClassifiers;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки пустого прямоугольника
    /// </summary>
    public class RectangleDraw : AreaDraw
    {
        internal RectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal RectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate!.Width ?? "1*", LegendGrid.LegendGridParameters.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate!.Height ?? "1*", LegendGrid.LegendGridParameters.CellHeight);
        /// <inheritdoc/>
        public override void Draw()
        {
            DrawRectangle(RectangleWidth, RectangleHeight);
        }

        private protected Polyline DrawRectangle(double width, double height, string? layer = null, double brightnessshift = 0d)
        {
            Polyline rectangle = new Polyline();
            rectangle.AddVertexAt(0, GetRelativePoint(-width / 2, -height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(1, GetRelativePoint(-width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, GetRelativePoint(width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(3, GetRelativePoint(width / 2, -height / 2), 0, 0d, 0d);
            rectangle.Closed = true;
            string separator = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].Separator;
            if (layer != null)
                LayerChecker.Check($"{LayerWrapper.StandartPrefix}{separator}{layer}"); //ПОКА ЗАВЯЗАНО НА ЧЕКЕР ИЗ ДРУГОГО МОДУЛЯ. ПРОАНАЛИЗИРОВАТЬ ВОЗМОЖНОСТИ ОПТИМИЗАЦИИ
            rectangle.Layer = layer == null ? Layer.BoundLayer.Name : $"{LayerWrapper.StandartPrefix}{separator}{layer}";
            bool success = LayerPropertiesDictionary.TryGetValue(rectangle.Layer, out LayerProps? lp);
            if (success)
            {
                rectangle.LinetypeScale = lp!.LTScale;
                rectangle.ConstantWidth = lp!.ConstantWidth;
            }
            rectangle.Color = BrightnessShift(brightnessshift);
            EntitiesList.Add(rectangle);
            return rectangle;
        }
    }
}
