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
        string FenceLayer => LegendDrawTemplate?.FenceLayer ?? Layer.BoundLayer.Name;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate?.FenceWidth ?? "1*", LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate?.FenceHeight ?? "1*", LegendGrid.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangle = new()
            { 
                DrawRectangle(RectangleWidth, RectangleHeight, brightnessshift: LegendDrawTemplate?.InnerBorderBrightness ?? 0) 
            };
            DrawHatch(rectangle,
                patternname: LegendDrawTemplate?.InnerHatchPattern ?? DefaultHatchPatternName,
                angle: LegendDrawTemplate?.InnerHatchAngle ?? 0,
                patternscale: LegendDrawTemplate?.InnerHatchScale ?? 1,
                increasebrightness: LegendDrawTemplate?.InnerHatchBrightness ?? 0
                );
            DrawRectangle(FenceWidth, FenceHeight, FenceLayer);
        }
    }
}
