//System

//Modules
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NanocadUtilities;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки сплошной линии с символьным (буквенным) обозначением
    /// </summary>
    public class MarkedSolidLineDraw : MarkedLineDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                var formatter = LoaderCore.LoaderExtension.ServiceProvider.GetService<IEntityFormatter>();
                formatter?.FormatEntity(line, Layer.LayerInfo.TrueName);
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
}
