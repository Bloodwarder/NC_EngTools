//System
using LayersIO.DataTransfer;

//Modules
using LayerWorks.LayerProcessing;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки заштрихованного прямоугольника
    /// </summary>
    public class HatchedRectangleDraw : RectangleDraw
    {
        public HatchedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public HatchedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangle = new() { DrawRectangle(RectangleWidth,
                                                             RectangleHeight,
                                                             brightnessshift: LegendDrawTemplate?.InnerBorderBrightness ?? 0) };
            DrawHatch(rectangle,
                patternname: LegendDrawTemplate?.InnerHatchPattern ?? DefaultHatchPatternName,
                angle: LegendDrawTemplate?.InnerHatchAngle ?? 0,
                patternscale: LegendDrawTemplate?.InnerHatchScale ?? 1,
                increasebrightness: LegendDrawTemplate?.InnerHatchBrightness ?? 0
                );
        }
    }
}
