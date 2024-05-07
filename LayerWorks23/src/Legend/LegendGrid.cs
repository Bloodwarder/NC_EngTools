using System.Collections.Generic;
using System.Linq;
using LayerWorks.LayerProcessing;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGrid
    {
        
        internal static LegendGridParameters LegendGridParameters { get; set; } = new LegendGridParameters();
        internal double CellWidth => LegendGridParameters.CellWidth;
        internal double CellHeight => LegendGridParameters.CellHeight;
        internal double WidthInterval => LegendGridParameters.WidthInterval;
        internal double HeightInterval => LegendGridParameters.HeightInterval;
        internal double TextWidth => LegendGridParameters.TextWidth;
        internal double TextHeight => LegendGridParameters.TextHeight;


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

        internal LegendGrid(IEnumerable<LegendGridCell> cells, Point3d basepoint, LegendGridParameters parameters) : this(cells, basepoint)
        {
            LegendGridParameters = parameters;
        }

        private void AddRow(LegendGridRow row)
        {
            row.ParentGrid = this;
            Rows.Add(row);
        }

        private void AddCells(LegendGridCell cell)
        {
            this[cell.Layer.LayerInfo.MainName].AddCell(cell);
            if (cell.ParentRow!.LegendData!.IgnoreLayer)
                return;
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
            // Выбрать и сортировать слои без метки игнорирования
            Rows = Rows.Where(r => !r.LegendData!.IgnoreLayer).ToList();
            Rows.Sort();
            // Выбрать разделы и вставить их названия в нужные места таблицы
            List<string> labels = Rows.Select(r => r.LegendEntityChapterName!).Distinct().ToList();
            foreach (var label in labels)
            {
                LegendGridRow row = new()
                {
                    ParentGrid = this,
                    Label = LegendInfoTable.Dictionary[label],
                    ItalicLabel = true
                };
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendEntityChapterName! == label).Min()!), row);
            }
            // И то же самое для подразделов
            var sublabels = Rows.Select(r => r.LegendData!.SubLabel).Where(s => s != null).Distinct().ToList();
            foreach (var label in sublabels)
            {
                LegendGridRow row = new LegendGridRow
                {
                    ParentGrid = this,
                    Label = label
                };
                var labeledRows = Rows.Where(r => r.LegendData!.SubLabel == label).ToList();
                int minindex = Rows.IndexOf(labeledRows.Min()!);
                int maxindex = Rows.IndexOf(labeledRows.Max()!);
                for (int i = minindex; i < maxindex; i++)
                {
                    Rows[i].IsSublabeledList = true;
                }
                Rows.Insert(minindex, row);

            }
            LegendGridRow gridLabel = new LegendGridRow
            {
                ParentGrid = this,
                Label = $"{{\\fArial|b1|i0|c204|p34;{"Инженерная инфраструктура".ToUpper()}}}"   // ВРЕМЕННО, ПОТОМ ОБРАБОТАТЬ КАЖДУЮ ТАБЛИЦУ В КОМПОНОВЩИКЕ
            };
            if (Rows.Count != 0)
                Rows.Insert(0, gridLabel);

            // Назначить целочисленные Y координаты каждому ряду таблицы

            for (int i = 0; i < Rows.Count; i++)
                Rows[i].AssignY(i);
        }
        private void ProcessColumns()
        {
            // Назначить целочисленные X координаты ячейкам таблицы на основе их статусов
            List<string?> statuses = Cells.Select(c => c.Layer.LayerInfo.Status).Distinct().ToList();
            _columns = statuses.Count;
            statuses.Sort();
            for (int i = 0; i < statuses.Count; i++)
            {
                foreach (LegendGridCell cell in Cells.Where(c => c.Layer.LayerInfo.Status == statuses[i]))
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
        // Индексатор, создающий строку при её отсутствии
        internal LegendGridRow this[string mainname]
        {
            get
            {
                var rows = Rows.Where(r => r.LegendEntityClassName == mainname);
                if (rows.Any())
                {
                    return rows.FirstOrDefault()!;
                }
                else
                {
                    LegendGridRow row = new(mainname)
                    {
                        ParentGrid = this
                    };
                    AddRow(row);
                    return row;
                }
            }
        }
    }
}
