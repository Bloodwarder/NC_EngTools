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
    /// Класс для отрисовки двух прямоугольников с двумя штриховками
    /// </summary>
    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate?.FenceLayer ?? Layer.BoundLayer.Name;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate?.FenceWidth ?? "1*", LegendGrid.LegendGridParameters.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate?.FenceHeight ?? "1*", LegendGrid.LegendGridParameters.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangles = new()
            {
                DrawRectangle
                    (
                    RectangleWidth,
                    RectangleHeight,
                    brightnessshift: LegendDrawTemplate?.InnerBorderBrightness ?? 0d
                    )
                };
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate?.InnerHatchPattern ?? DefaultHatchPatternName,
                angle: LegendDrawTemplate?.InnerHatchAngle ?? 0,
                patternscale: LegendDrawTemplate?.InnerHatchScale ?? 1,
                increasebrightness: LegendDrawTemplate?.InnerHatchBrightness ?? 0
                );
            rectangles.Add(DrawRectangle(FenceWidth, FenceHeight, FenceLayer));
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate?.OuterHatchPattern ?? DefaultHatchPatternName,
                angle: LegendDrawTemplate?.OuterHatchAngle ?? 0,
                patternscale: LegendDrawTemplate?.OuterHatchScale ?? 1,
                increasebrightness: LegendDrawTemplate?.OuterHatchBrightness ?? 0
                );
        }
    }
}
