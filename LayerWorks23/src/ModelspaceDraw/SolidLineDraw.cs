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
        internal SolidLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.LayerInfo.Name;
            bool success = LayerPropertiesDictionary.TryGetValue(Layer.LayerInfo.TrueName, out LayerProps? lp);
            if (success)
            {
                pl.LinetypeScale = lp?.LTScale ?? default;
                pl.ConstantWidth = lp?.ConstantWidth ?? default;
            }
            EntitiesList.Add(pl);
        }
    }
}
