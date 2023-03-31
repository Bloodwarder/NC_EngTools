using System;
using System.Collections.Generic;
using System.Linq;
using ExternalData;
using LayerProcessing;

namespace Legend
{
    class LegendGrid
    {
        List<LegendGridRow> Rows = new List<LegendGridRow>();

        private void addRow(LegendGridRow row)
        {
            row.ParentGrid = this;
            Rows.Add(row);
        }

        internal void AddCell(LegendGridCell cell)
        {
            this[cell.Layer.MainName].AddCell(cell);
        }

        internal void AddCell(IEnumerable<LegendGridCell> cells)
        {
            foreach (var cell in cells)
                AddCell(cell);
        }

        private void processCells(IEnumerable<LegendGridCell> cells)
        {
            foreach (LegendGridCell cell in cells)
                AddCell(cell);
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

    class GridsComposer
    {
        
        List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();
        TableFilter _filter;
        GridsComposer(IEnumerable<LegendGridCell> cells, TableFilter filter)
        {
            Cells.AddRange(cells);
            _filter = filter;
        }

        internal void Compose()
        {
            switch(_filter)
            {
                case TableFilter.ExistingOnly:
                    addGrid(c => c.Layer.BuildStatus == "сущ");
                    break;

                case TableFilter.InternalOnly:

                    break;

                case TableFilter.InternalAndExternal:

                    break;

                case TableFilter.InternalAndSeparatedExternal:

                    break;

                case TableFilter.Full:
                    
                    break;
            }

        }
        LegendGrid addGrid(Func<LegendGridCell,bool> predicate)
        {
            List<LegendGridCell> cells = Cells.Where(predicate).ToList();
            LegendGrid grid = new LegendGrid();
            grid.AddCell(cells);
            Grids.Add(grid);
        }
    }
    internal class LegendGridRow : IComparable
    {
        internal string LegendEntityClassName { get; private set; }
        internal string LegendEntityChapterName { get; private set; }
        internal LegendData LegendData { get; private set; }
        internal string Label { get => _islabelrow ? label : LegendData.Label; set => label=value; }
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

        List<LegendGridCell> _cells { get; set; } = new List<LegendGridCell>();
        internal LegendGrid ParentGrid { get; set; }

        public void AddCell(LegendGridCell legendGridCell)
        {
            _cells.Add(legendGridCell);
            legendGridCell.ParentRow = this;
            if (LegendEntityChapterName != string.Empty)
                return;
            LegendEntityChapterName = legendGridCell.Layer.EngType;
        }

        public int CompareTo(object obj)
        {
            LegendGridRow lgr = obj as LegendGridRow;
            return this.LegendData.Rank.CompareTo(lgr.LegendData.Rank);
        }
    }

    class LegendGridCell
    {
        LegendGridCell(LayerParser layer)
        {
            Layer=layer;
        }

        internal LegendGridRow ParentRow { get; set; }
        internal LayerParser Layer { get; set; }
    }

    class LegendGridRowComparer : IComparer<LegendGridRow>
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