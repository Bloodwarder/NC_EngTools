using System;
using System.Collections.Generic;
using System.Reflection;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LayerWorks.ModelspaceDraw;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGridCell : ICloneable
    {
        List<LegendObjectDraw> _draw = new List<LegendObjectDraw>();
        private LegendDrawTemplate _template;

        internal LegendGridCell(RecordLayerParser layer)
        {
            Layer = layer;
            _template = LayerLegendDrawDictionary.GetValue(layer.LayerInfo.TrueName, out _);
        }

        internal LegendGrid ParentGrid { get; set; }
        internal LegendGridRow ParentRow { get; set; }
        internal RecordLayerParser Layer { get; set; }
        internal CellTableIndex TableIndex = new CellTableIndex();

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
            LegendObjectDraw lod = Activator.CreateInstance
                (
                Assembly.GetCallingAssembly().FullName,
                string.Concat("ModelspaceDraw.", _template.DrawTemplate, "Draw")
                )
                .Unwrap() as LegendObjectDraw;

            lod.LegendDrawTemplate = _template;
            lod.Layer = Layer;
            double x = ParentGrid.BasePoint.X + TableIndex.X * (ParentGrid.CellWidth + ParentGrid.WidthInterval) + ParentGrid.CellWidth / 2;
            double y = ParentGrid.BasePoint.Y - TableIndex.Y * (ParentGrid.CellHeight + ParentGrid.HeightInterval) + ParentGrid.CellHeight / 2;
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
