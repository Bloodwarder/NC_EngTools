using LayerWorks.LayerProcessing;
using LayerWorks.ModelspaceDraw;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;

namespace LayerWorks.Legend
{
    internal class LegendGridRow : IComparable
    {
        internal string LegendEntityClassName { get; private set; }
        internal string LegendEntityChapterName { get; private set; }
        internal LegendData LegendData { get; private set; }
        internal string Label { get => _islabelrow ? _label : LegendData.Label; set => _label = value; }
        /// <summary>
        /// Помечает, что заголовок легенды является перечислением и в конце нужно поставить ";"
        /// </summary>
        internal bool IsSublabeledList { get; set; } = false;
        internal int YIndex;
        internal bool ItalicLabel = false;
        private readonly bool _islabelrow = false;
        private string _label;
        private ObjectDraw _draw;

        internal LegendGridRow()
        {
            _islabelrow = true;
        }
        internal LegendGridRow(LegendGridCell cell)
        {
            LegendEntityClassName = cell.Layer.MainName;
            LegendEntityChapterName = cell.Layer.EngType;
        }
        internal LegendGridRow(string mainname)
        {
            LegendEntityClassName = mainname;
            LegendData = LayerLegendDictionary.GetValue(mainname, out bool success);
            if (!success)
                throw new Exception($"Нет данных для слоя {string.Concat(LayerWrapper.StandartPrefix, mainname)}");
        }
        internal List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        internal LegendGrid ParentGrid { get; set; }

        public void AddCell(LegendGridCell legendGridCell)
        {
            Cells.Add(legendGridCell);
            legendGridCell.ParentRow = this;
            if (LegendEntityChapterName != null)
                return;
            LegendEntityChapterName = legendGridCell.Layer.EngType;
        }

        internal void AssignY(int y)
        {
            YIndex = y;
            foreach (LegendGridCell cell in Cells)
                cell.AssignY(y);
        }
        public List<Entity> Draw()
        {
            string label = _islabelrow ? _label : IsSublabeledList ? string.Concat(LegendData.Label, ";") : LegendData.Label; //переписать эту лесенку
            _draw = new LabelTextDraw(
                new Point2d(
                ParentGrid.BasePoint.X + (ParentGrid.Width - ParentGrid.TextWidth) + ParentGrid.WidthInterval,
                ParentGrid.BasePoint.Y - YIndex * (ParentGrid.CellHeight + ParentGrid.HeightInterval) + ParentGrid.CellHeight / 2),
                label,
                ItalicLabel);
            List<Entity> list = new List<Entity>();
            _draw.Draw();
            list.AddRange(_draw.EntitiesList);
            return list;
        }

        public int CompareTo(object obj)
        {
            LegendGridRow lgr = obj as LegendGridRow;
            return LegendData.Rank.CompareTo(lgr.LegendData.Rank);
        }
    }
}
