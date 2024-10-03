using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LayerWorks.ModelspaceDraw;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGridCell : ICloneable
    {
        List<LegendObjectDraw> _draw = new List<LegendObjectDraw>();
        private LegendDrawTemplate? _template;

        internal LegendGridCell(RecordLayerWrapper layer)
        {
            Layer = layer;

            var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendDrawTemplate>>(); // TODO: перенести в статику
            bool success = service.TryGet(layer.LayerInfo.TrueName, out LegendDrawTemplate? ldt);
            if (success)
                _template = ldt;
            else
                throw new Exception("Нет шаблона для отрисовки");
        }

        internal LegendGrid? ParentGrid { get; set; }
        internal LegendGridRow? ParentRow { get; set; }
        internal RecordLayerWrapper Layer { get; set; }
        internal CellTableIndex TableIndex;

        internal void AssignX(int x)
        {
            TableIndex.X = x;
        }
        internal void AssignY(int y)
        {
            TableIndex.Y = y;
        }
        public void CreateDrawObject()
        {
            string typeName = string.Concat("ModelspaceDraw.", _template!.DrawTemplate, "Draw");
            LegendObjectDraw? lod = Activator.CreateInstance(Assembly.GetCallingAssembly().FullName!, typeName)!.Unwrap() as LegendObjectDraw;
            if (lod == null)
                throw new Exception($"Отсутствует тип с именем {typeName}");

            lod.LegendDrawTemplate = _template;
            lod.Layer = Layer;
            double x = ParentGrid!.BasePoint.X + TableIndex.X * (LegendGrid.CellWidth + LegendGrid.WidthInterval) + LegendGrid.CellWidth / 2;
            double y = ParentGrid!.BasePoint.Y - TableIndex.Y * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2;
            lod.Basepoint = new Point2d(x, y);
            _draw.Add(lod);
        }

        public List<Entity> Draw()
        {
            List<Entity> list = new List<Entity>();
            foreach (LegendObjectDraw lod in _draw)
            {
                lod.Draw();
                list.AddRange(lod.EntitiesList);
            }
            return list;
        }
        public object Clone()
        {
            LegendGridCell lgc = (LegendGridCell)MemberwiseClone();
            lgc._draw = new List<LegendObjectDraw>();
            lgc.TableIndex = new CellTableIndex();
            lgc.ParentRow = null;
            return lgc;
        }
    }
}
