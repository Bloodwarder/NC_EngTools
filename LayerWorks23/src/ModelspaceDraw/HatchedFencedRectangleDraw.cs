//System
using System.Collections.Generic;

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки двух прямоугольников с двумя штриховками
    /// </summary>
    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.LegendGridParameters.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceHeight, LegendGrid.LegendGridParameters.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedFencedRectangleDraw() { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangles = new List<Polyline>
                {
                DrawRectangle
                    (
                    RectangleWidth,
                    RectangleHeight,
                    brightnessshift: LegendDrawTemplate.InnerBorderBrightness
                    )
                };
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
            rectangles.Add(DrawRectangle(FenceWidth, FenceHeight, FenceLayer));
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate.OuterHatchPattern,
                angle: LegendDrawTemplate.OuterHatchAngle,
                patternscale: LegendDrawTemplate.OuterHatchScale,
                increasebrightness: LegendDrawTemplate.OuterHatchBrightness
                );
        }
    }
}
