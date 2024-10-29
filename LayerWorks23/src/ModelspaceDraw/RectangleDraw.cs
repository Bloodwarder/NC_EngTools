//System

//Modules
//nanoCAD
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки пустого прямоугольника
    /// </summary>
    public class RectangleDraw : AreaDraw
    {
        public RectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public RectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate!.Width ?? "1*", LegendGrid.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate!.Height ?? "1*", LegendGrid.CellHeight);
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
            string separator = NameParser.Current.Separator;
            if (layer != null)
                LayerChecker.Check($"{NameParser.Current.Prefix}{separator}{layer}"); //ПОКА ЗАВЯЗАНО НА ЧЕКЕР ИЗ ДРУГОГО МОДУЛЯ. ПРОАНАЛИЗИРОВАТЬ ВОЗМОЖНОСТИ ОПТИМИЗАЦИИ
            rectangle.Layer = layer == null ? Layer.BoundLayer.Name : $"{NameParser.Current.Prefix}{separator}{layer}";

            var formatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>();
            formatter?.FormatEntity(rectangle, Layer.LayerInfo.TrueName);

            rectangle.Color = BrightnessShift(brightnessshift);
            EntitiesList.Add(rectangle);
            return rectangle;
        }
    }
}
