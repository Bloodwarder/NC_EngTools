//System

//Modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using LoaderCore.NanocadUtilities;
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
        public MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                var formatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>();
                formatter?.FormatEntity(line, LayerWrapper.LayerInfo.TrueName);
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
}
