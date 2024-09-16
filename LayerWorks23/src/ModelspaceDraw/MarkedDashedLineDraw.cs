//System

//Modules
//nanoCAD
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Teigha.DatabaseServices;
using Teigha.Geometry;

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
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
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
                bool ltgetsuccess = LayerChecker.TryFindLinetype("ACAD_ISO02W100", out ObjectId lineTypeId);
                if (ltgetsuccess)
                    line.LinetypeId = lineTypeId;
                line.LinetypeScale = 0.3d;
            }
        }
    }
}
