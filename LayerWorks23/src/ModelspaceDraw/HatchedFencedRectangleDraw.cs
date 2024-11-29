//System

//Modules
//nanoCAD
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using NameClassifiers;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки двух прямоугольников с двумя штриховками
    /// </summary>
    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate?.FenceLayer != null ? 
                             $"{NameParser.Current.Prefix}{NameParser.Current.Separator}{LegendDrawTemplate?.FenceLayer}" : 
                             LayerWrapper.BoundLayer.Name;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate?.FenceWidth ?? "1*", LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate?.FenceHeight ?? "1*", LegendGrid.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected override void CreateEntities()
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
