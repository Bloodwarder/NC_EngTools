//System
using System.Collections.Generic;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

using LayerWorks.LayerProcessing;
using LayerWorks.Legend;

namespace LayerWorks.ModelspaceDraw
{

    /// <summary>
    /// Класс для отрисовки заштрихованного круга
    /// </summary>
    public class HatchedCircleDraw : AreaDraw
    {
        internal double Radius => LegendDrawTemplate?.Radius ?? LegendGrid.LegendGridParameters.CellHeight;
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> circle = new() { DrawCircle(Radius) };
            DrawHatch(circle,
                patternname: LegendDrawTemplate?.InnerHatchPattern ?? DefaultHatchPatternName,
                angle: LegendDrawTemplate?.InnerHatchAngle ?? 0,
                patternscale: LegendDrawTemplate?.InnerHatchScale ?? 1,
                increasebrightness: LegendDrawTemplate?.InnerHatchBrightness ?? 0
                );
        }

        private protected Polyline DrawCircle(double radius, string? layer = null)
        {
            Polyline circle = new();
            circle.AddVertexAt(0, GetRelativePoint(0, radius / 2), 1, 0d, 0d);
            circle.AddVertexAt(1, GetRelativePoint(0, -radius / 2), 1, 0d, 0d);
            circle.AddVertexAt(2, GetRelativePoint(0, radius / 2), 0, 0d, 0d);
            circle.Closed = true;
            circle.Layer = layer ?? Layer.BoundLayer.Name;
            bool success = LayerPropertiesDictionary.TryGetValue(circle.Layer, out LayerProps? lp);
            if (success)
            {
                circle.LinetypeScale = lp!.LTScale;
                circle.ConstantWidth = lp!.ConstantWidth;
            }

            EntitiesList.Add(circle);
            return circle;
        }
    }
}
