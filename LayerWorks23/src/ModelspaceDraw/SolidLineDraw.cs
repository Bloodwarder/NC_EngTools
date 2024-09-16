//System
using Microsoft.Extensions.DependencyInjection;
//Modules
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

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
            var formatter = LoaderCore.LoaderExtension.ServiceProvider.GetService<IEntityFormatter>();
            formatter?.FormatEntity(pl, Layer.LayerInfo.TrueName);
            EntitiesList.Add(pl);
        }
    }
}
