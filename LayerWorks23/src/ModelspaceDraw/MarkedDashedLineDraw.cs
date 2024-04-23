//System
using System.Collections.Generic;

//Modules
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LayerWorks.LayerProcessing;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки пунктирной линии с символьным (буквенным) обозначением
    /// </summary>
    public class MarkedDashedLineDraw : MarkedLineDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public MarkedDashedLineDraw() { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerWrapper layer = null) : base(basepoint, layer) { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.TryGetValue(Layer.LayerInfo.TrueName, out _, true).ConstantWidth;
                LayerChecker.FindLinetype("ACAD_ISO02W100", out bool ltgetsuccess);
                if (ltgetsuccess)
                    line.Linetype = "ACAD_ISO02W100";
                line.LinetypeScale = 0.3d;
            }
        }
    }
}
