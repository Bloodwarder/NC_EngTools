//System
using System.Collections.Generic;

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LayersIO.DataTransfer;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки заштрихованного прямоугольника внутри другого прямоугольника-ограждения
    /// </summary>
    public class FencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.LegendGridParameters.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceHeight, LegendGrid.LegendGridParameters.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public FencedRectangleDraw() { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { DrawRectangle(RectangleWidth, RectangleHeight, brightnessshift: LegendDrawTemplate.InnerBorderBrightness) };
            DrawHatch(rectangle,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
            DrawRectangle(FenceWidth, FenceHeight, FenceLayer);
        }
    }
}
