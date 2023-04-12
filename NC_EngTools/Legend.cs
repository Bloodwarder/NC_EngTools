using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalData;
using LayerProcessing;
using ModelspaceDraw;
using Teigha.Geometry;

namespace Legend
{
    class LegendGrid
    {
        List<LegendGridRow> Rows = new List<LegendGridRow>();
        List<LegendGridCell> Cells = new List<LegendGridCell>();
        internal double Width { get; }
        internal Point3d BasePoint = new Point3d();
        private void addRow(LegendGridRow row)
        {
            row.ParentGrid = this;
            Rows.Add(row);
        }

        internal void AddCells(LegendGridCell cell)
        {
            this[cell.Layer.MainName].AddCell(cell);
            Cells.Add(cell);
        }

        internal void AddCells(IEnumerable<LegendGridCell> cells)
        {
            foreach (var cell in cells)
                AddCells(cell);
        }

        private void processCells(IEnumerable<LegendGridCell> cells)
        {
            foreach (LegendGridCell cell in cells)
                AddCells(cell);
        }
        private void processRows()
        {
            Rows = Rows.Where(r => !r.LegendData.IgnoreLayer).ToList();
            Rows.Sort();
            var labels = Rows.Select(r => r.LegendEntityChapterName).Distinct().ToList();
            foreach (var label in labels)
            {
                LegendGridRow row = new LegendGridRow();
                row.Label= LegendInfoTable.Dictionary[label];
                row.ItalicLabel=true;
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendEntityChapterName == label).Min()), row);
            }
            var sublabels = Rows.Select(r => r.LegendData.SubLabel).Distinct().ToList();
            foreach (var label in sublabels)
            {
                LegendGridRow row = new LegendGridRow();
                row.Label= label;
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendEntityChapterName == label).Min()), row);
            }
            for (int i = 0; i < Rows.Count; i++)
                Rows[i].AssignY(i);
        }
        private void processColumns()
        {
            List<Status> statuses = Cells.Select(c => c.Layer.BuildStatus).Distinct().ToList();
            statuses.Sort();
            for (int i = 0; i < statuses.Count; i++)
            {
                foreach (LegendGridCell cell in Cells.Where(c => c.Layer.BuildStatus == statuses[i]))
                    cell.AssignX(i);
            }
        }

        internal void Assemble()
        {
            processRows();
            processColumns();
        }
        //индексатор
        internal LegendGridRow this[string mainname]
        {
            get
            {
                var rows = Rows.Where(r => r.LegendEntityClassName == mainname);
                if (rows.Count() > 0)
                {
                    return rows.FirstOrDefault();
                }
                else
                {
                    LegendGridRow row = new LegendGridRow(mainname);
                    addRow(row);
                    return row;
                }
            }
        }
    }

    internal class GridsComposer
    {

        internal List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        internal List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();
        TableFilter _filter;
        internal GridsComposer(IEnumerable<LegendGridCell> cells, TableFilter filter)
        {
            Cells.AddRange(cells);
            _filter = filter;
        }

        internal void Compose(Point3d basepoint)
        {
            switch (_filter)
            {
                case TableFilter.ExistingOnly:
                    addGrid(c => c.Layer.BuildStatus == Status.Existing);
                    break;

                case TableFilter.InternalOnly:
                    addGrid(c => c.Layer.BuildStatus == (Status.Existing|Status.Deconstructing|Status.Planned));
                    break;

                case TableFilter.InternalAndExternal:
                    addGrid(c => c.Layer.BuildStatus == (Status.Existing|Status.Deconstructing|Status.Planned));
                    addGrid(c => c.Layer.BuildStatus == (Status.NSDeconstructing|Status.NSPlanned));

                    break;

                case TableFilter.InternalAndSeparatedExternal:
                    addGrid(c => c.Layer.BuildStatus == (Status.Existing|Status.Deconstructing|Status.Planned));
                    List<string> extprojects = Cells.Where(c => c.Layer.ExtProjectName!=string.Empty).Select(c => c.Layer.ExtProjectName).Distinct().ToList();
                    foreach (string extproject in extprojects)
                        addGrid(c => c.Layer.ExtProjectName == extproject);
                    break;

                case TableFilter.Full:
                    addGrid(c => true);
                    break;
            }

        }
        void addGrid(Func<LegendGridCell, bool> predicate)
        {
            List<LegendGridCell> cells = Cells.Where(predicate).ToList();
            LegendGrid grid = new LegendGrid();
            grid.AddCells(cells);
            Grids.Add(grid);
        }
    }
    internal class LegendGridRow : IComparable
    {
        internal string LegendEntityClassName { get; private set; }
        internal string LegendEntityChapterName { get; private set; }
        internal LegendData LegendData { get; private set; }
        internal string Label { get => _islabelrow ? label : LegendData.Label; set => label=value; }
        internal int YIndex;
        internal bool ItalicLabel = false;
        private bool _islabelrow = false;
        private string label;

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
                throw new System.Exception();
        }
        internal List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        internal LegendGrid ParentGrid { get; set; }

        public void AddCell(LegendGridCell legendGridCell)
        {
            Cells.Add(legendGridCell);
            legendGridCell.ParentRow = this;
            if (LegendEntityChapterName != string.Empty)
                return;
            LegendEntityChapterName = legendGridCell.Layer.EngType;
        }

        internal void AssignY(int y)
        {
            YIndex = y;
            foreach (LegendGridCell cell in Cells)
                cell.AssignY(y);
        }
        public int CompareTo(object obj)
        {
            LegendGridRow lgr = obj as LegendGridRow;
            return this.LegendData.Rank.CompareTo(lgr.LegendData.Rank);
        }
    }


    internal class LegendGridCell
    {
        List<LegendObjectDraw> _draw = new List<LegendObjectDraw>();
        internal LegendGridCell(RecordLayerParser layer)
        {
            Layer=layer;
            LegendDrawTemplate template = LayerLegendDrawDictionary.GetValue(layer.TrueName, out _);
            LegendObjectDraw lod = Activator.CreateInstance("NC_EngTools", string.Concat(template.DrawTemplate, "Draw")).Unwrap() as LegendObjectDraw;
            lod.LegendDrawTemplate = template;
            lod.Layer = layer;
            
            _draw.Add(lod);
        }

        internal LegendGridRow ParentRow { get; set; }
        internal LayerParser Layer { get; set; }
        internal CellTableIndex TableIndex = new CellTableIndex();

        internal void AssignX(int x)
        {
            TableIndex.X=x;
        }
        internal void AssignY(int y)
        {
            TableIndex.Y=y;
        }

    }

    internal class LegendGridRowComparer : IComparer<LegendGridRow>
    {
        public int Compare(LegendGridRow x, LegendGridRow y)
        {
            if (x.LegendData.Rank > y.LegendData.Rank)
                return 1;
            if (x.LegendData.Rank < y.LegendData.Rank)
                return -1;
            else
                return 0;
        }
    }



    public static class LegendInfoTable
    {
        internal static Dictionary<string, string> Dictionary = new Dictionary<string, string>();

        static LegendInfoTable()
        {
            Dictionary.Add("ВО", "водоотведение");
            Dictionary.Add("ВС", "водоснабжение");
            Dictionary.Add("ТС", "теплоснабжение");
            Dictionary.Add("ГС", "газоснабжение");
            Dictionary.Add("ДК", "дождевая канализация");
            Dictionary.Add("СС", "связь");
            Dictionary.Add("ЭС", "электроснабжение");
            Dictionary.Add("ТТ", "трубопроводный транспорт");
            Dictionary.Add("ЖД", "сети инфраструктуры железной дороги");
            Dictionary.Add("ИН", "иные");

        }
    }

    public struct CellTableIndex
    {
        public int X;
        public int Y;
    }

    public struct LegendData
    {
        public int Rank;
        public string Label;
        public string SubLabel;
        public bool IgnoreLayer;
    }

    public enum TableFilter
    {
        ExistingOnly,
        Full,
        InternalOnly,
        InternalAndExternal,
        InternalAndSeparatedExternal
    }
}
