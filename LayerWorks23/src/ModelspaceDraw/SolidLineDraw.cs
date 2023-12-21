//System

//Modules
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

using LayerWorks.LayerProcessing;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки непрерывной линии
    /// </summary>
    public class SolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public SolidLineDraw() { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.OutputLayerName;
            LayerProps lp = LayerPropertiesDictionary.GetValue(Layer.TrueName, out bool success);
            if (success)
            {
                pl.LinetypeScale = lp.LTScale;
                pl.ConstantWidth = lp.ConstantWidth;
            }
            EntitiesList.Add(pl);
        }
    }
}
