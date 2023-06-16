using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalData;
using LayerProcessing;
using ModelspaceDraw;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Legend
{
    class LegendGrid
    {
        internal static double CellWidth { get; } = 30;
        internal static double CellHeight { get; } = 9;
        internal static double WidthInterval { get; } = 5;
        internal static double HeightInterval { get; } = 5;
        internal static double TextWidth { get; } = 150;
        internal static double TextHeight { get; } = 4;


        internal List<LegendGridRow> Rows = new List<LegendGridRow>();
        internal List<LegendGridCell> Cells = new List<LegendGridCell>();
        internal double Width { get => _columns * CellWidth + _columns * WidthInterval + TextWidth; }
        internal Point3d BasePoint = new Point3d();
        private int _columns;

        internal LegendGrid(IEnumerable<LegendGridCell> cells, Point3d basepoint)
        {
            AddCells(cells);
            BasePoint = basepoint;
        }
        private void AddRow(LegendGridRow row)
        {
            row.ParentGrid = this;
            Rows.Add(row);
        }

        private void AddCells(LegendGridCell cell)
        {
            this[cell.Layer.MainName].AddCell(cell);
            cell.ParentGrid = this;
            Cells.Add(cell);
        }

        private void AddCells(IEnumerable<LegendGridCell> cells)
        {
            foreach (var cell in cells)
                AddCells(cell);
        }
        private void ProcessRows()
        {
            Rows = Rows.Where(r => !r.LegendData.IgnoreLayer).ToList();
            Rows.Sort();
            var labels = Rows.Select(r => r.LegendEntityChapterName).Distinct().ToList();
            foreach (var label in labels)
            {
                LegendGridRow row = new LegendGridRow
                {
                    ParentGrid = this,
                    Label = LegendInfoTable.Dictionary[label],
                    ItalicLabel = true
                };
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendEntityChapterName == label).Min()), row);
            }
            var sublabels = Rows.Select(r => r.LegendData.SubLabel).Where(s => s != null).Distinct().ToList();
            foreach (var label in sublabels)
            {
                LegendGridRow row = new LegendGridRow
                {
                    ParentGrid = this,
                    Label = label
                };
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendData.SubLabel == label).Min()), row);
            }
            for (int i = 0; i < Rows.Count; i++)
                Rows[i].AssignY(i);
        }
        private void ProcessColumns()
        {
            List<Status> statuses = Cells.Select(c => c.Layer.BuildStatus).Distinct().ToList();
            _columns = statuses.Count;
            statuses.Sort();
            for (int i = 0; i < statuses.Count; i++)
            {
                foreach (LegendGridCell cell in Cells.Where(c => c.Layer.BuildStatus == statuses[i]))
                    cell.AssignX(i);
            }
        }

        private void ProcessDrawObjects()
        {
            foreach (LegendGridCell cell in Cells)
            {
                cell.CreateDrawObject();
            }
        }

        internal void Assemble()
        {
            ProcessRows();
            ProcessColumns();
            ProcessDrawObjects();
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
                    LegendGridRow row = new LegendGridRow(mainname)
                    {
                        ParentGrid = this
                    };
                    AddRow(row);
                    return row;
                }
            }
        }
    }

    internal class GridsComposer
    {

        internal List<LegendGridCell> SourceCells { get; set; } = new List<LegendGridCell>();
        internal List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();

        private readonly TableFilter _filter;
        Point3d _basepoint;
        internal GridsComposer(IEnumerable<LegendGridCell> cells, TableFilter filter)
        {
            SourceCells.AddRange(cells);
            _filter = filter;
        }

        internal void Compose(Point3d basepoint)
        {
            _basepoint = basepoint;
            switch (_filter)
            {
                case TableFilter.ExistingOnly:
                    AddGrid(c => c.Layer.BuildStatus == Status.Existing);
                    break;

                case TableFilter.InternalOnly:
                    AddGrid(c => c.Layer.BuildStatus == (Status.Existing | Status.Deconstructing | Status.Planned));
                    break;

                case TableFilter.InternalAndExternal:
                    AddGrid(c => c.Layer.BuildStatus == (Status.Existing | Status.Deconstructing | Status.Planned));
                    AddGrid(c => c.Layer.BuildStatus == (Status.NSDeconstructing | Status.NSPlanned));

                    break;

                case TableFilter.InternalAndSeparatedExternal:
                    AddGrid(c => c.Layer.BuildStatus == (Status.Existing | Status.Deconstructing | Status.Planned));
                    List<string> extprojects = SourceCells.Where(c => c.Layer.ExtProjectName != string.Empty).Select(c => c.Layer.ExtProjectName).Distinct().ToList();
                    foreach (string extproject in extprojects)
                        AddGrid(c => c.Layer.ExtProjectName == extproject);
                    break;

                case TableFilter.Full:
                    AddGrid(c => true);
                    break;
            }

        }
        void AddGrid(Func<LegendGridCell, bool> predicate)
        {
            List<LegendGridCell> filteredcells = SourceCells.Where(predicate).ToList();
            List<LegendGridCell> cells = new List<LegendGridCell>();
            foreach (LegendGridCell cell in filteredcells)
            {
                cells.Add(cell.Clone() as LegendGridCell);
            }
            double deltax = Grids.Select(g => g.Width).Sum() + 500 * Grids.Count;
            LegendGrid grid = new LegendGrid(cells, new Point3d(_basepoint.X + deltax, _basepoint.Y, 0d));
            grid.Assemble();
            Grids.Add(grid);
        }
        internal List<Entity> DrawGrids()
        {
            List<Entity> entities = new List<Entity>();
            foreach (LegendGrid grid in Grids)
            {
                foreach (LegendGridCell cell in grid.Cells)
                {
                    entities.AddRange(cell.Draw());
                }
                foreach (LegendGridRow row in grid.Rows)
                {
                    entities.AddRange(row.Draw());
                }
            }
            return entities;
        }
    }
    internal class LegendGridRow : IComparable
    {
        internal string LegendEntityClassName { get; private set; }
        internal string LegendEntityChapterName { get; private set; }
        internal LegendData LegendData { get; private set; }
        internal string Label { get => _islabelrow ? _label : LegendData.Label; set => _label = value; }
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
                throw new System.Exception($"Нет данных для слоя {string.Concat(LayerParser.StandartPrefix, mainname)}");
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
            _draw = new LabelTextDraw(
                new Point2d(
                ParentGrid.BasePoint.X + (ParentGrid.Width - LegendGrid.TextWidth) + LegendGrid.WidthInterval,
                ParentGrid.BasePoint.Y - YIndex * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2),
                _islabelrow ? _label : LegendData.Label);
            List<Entity> list = new List<Entity>();
            _draw.Draw();
            list.AddRange(_draw.EntitiesList);
            return list;
        }

        public int CompareTo(object obj)
        {
            LegendGridRow lgr = obj as LegendGridRow;
            return this.LegendData.Rank.CompareTo(lgr.LegendData.Rank);
        }
    }


    internal class LegendGridCell : ICloneable
    {
        List<LegendObjectDraw> _draw = new List<LegendObjectDraw>();
        private LegendDrawTemplate _template;

        internal LegendGridCell(RecordLayerParser layer)
        {
            Layer = layer;
            _template = LayerLegendDrawDictionary.GetValue(layer.TrueName, out _);
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
            LegendObjectDraw lod = Activator.CreateInstance("NC_EngTools", string.Concat("ModelspaceDraw.", _template.DrawTemplate, "Draw")).Unwrap() as LegendObjectDraw;
            lod.LegendDrawTemplate = _template;
            lod.Layer = Layer;
            double x = ParentGrid.BasePoint.X + TableIndex.X * (LegendGrid.CellWidth + LegendGrid.WidthInterval) + LegendGrid.CellWidth / 2;
            double y = ParentGrid.BasePoint.Y - TableIndex.Y * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2;
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
