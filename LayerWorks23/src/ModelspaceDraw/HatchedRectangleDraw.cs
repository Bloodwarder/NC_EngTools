//System
using System.Collections.Generic;
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
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedRectangleDraw() { }

        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
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
        }
    }
}
