//System
using System.Collections.Generic;

//Modules
using LoaderCore.Utilities;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

using LayerWorks.LayerProcessing;

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
        public MarkedSolidLineDraw() { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer = null) : base(basepoint, layer) { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(Layer.LayerInfo.TrueName, out _, true).ConstantWidth;
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
}
