using LoaderCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGrid
    {
        private const string DefaultLegendHeader = "УСЛОВНЫЕ ОБОЗНАЧЕНИЯ";
        
        private static LegendGridParameters _legendGridParameters;
        private int _columns;

        static LegendGrid()
        {
            var config = LoaderCore.NcetCore.ServiceProvider.GetService<IConfiguration>();
            var lgp = config?.GetSection("LayerWorksConfiguration").GetValue<LegendGridParameters>("LegendGridParameters");
            _legendGridParameters = lgp ?? LegendGridParameters.GetDefault();
        }
        internal LegendGrid(IEnumerable<LegendGridCell> cells, Point3d basepoint, string name = DefaultLegendHeader)
        {
            AddCells(cells);
            BasePoint = basepoint;
            Name = name;
        }

        internal LegendGrid(IEnumerable<LegendGridCell> cells, Point3d basepoint, LegendGridParameters parameters, string name = DefaultLegendHeader) : this(cells, basepoint, name)
        {
            LegendGridParameters = parameters;
        }

        internal static double CellWidth => LegendGridParameters.CellWidth;
        internal static double CellHeight => LegendGridParameters.CellHeight;
        internal static double WidthInterval => LegendGridParameters.WidthInterval;
        internal static double HeightInterval => LegendGridParameters.HeightInterval;
        internal static double TextWidth => LegendGridParameters.TextWidth;
        internal static double TextHeight => LegendGridParameters.TextHeight;
        private static LegendGridParameters LegendGridParameters { get => _legendGridParameters; set => _legendGridParameters = value; }

        internal string Name { get; }
        internal List<LegendGridRow> Rows { get; private set; } = new();
        internal List<LegendGridCell> Cells { get; } = new();
        internal double Width { get => _columns * CellWidth + _columns * WidthInterval + TextWidth; }
        internal Point3d BasePoint { get; set; } = new Point3d();

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

        internal void Assemble()
        {
            ProcessRows();
            ProcessColumns();
            ProcessDrawObjects();
        }

        internal static void ReloadGridParameters()
        {
            _legendGridParameters = Configuration.LayerWorks.GetLegendGridParameters();
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
            var sublabels = Rows.Select(r => r.LegendData?.SubLabel).Where(s => s != null).Distinct().ToList();
            foreach (var label in sublabels)
            {
                LegendGridRow row = new()
                {
                    ParentGrid = this,
                    Label = label
                };
                var labeledRows = Rows.Where(r => r.LegendData?.SubLabel == label).ToList();
                int minindex = Rows.IndexOf(labeledRows.Min()!);
                int maxindex = Rows.IndexOf(labeledRows.Max()!);
                for (int i = minindex; i < maxindex; i++)
                {
                    Rows[i].IsSublabeledList = true;
                }
                Rows.Insert(minindex, row);

            }
            LegendGridRow gridLabel = new()
            {
                ParentGrid = this,
                Label = $"{{\\fArial|b1|i0|c204|p34;{Name.ToUpper()}}}"   // TODO: ПРОСКАКИВАЕТ ЗВЁЗДОЧКА! НАЙТИ МЕСТО, ГДЕ ЕЁ ЗАМЕНИТЬ! ИМЯ ДОБАВЛЕНО, ОСТАЛОСЬ ОБРАБОТАТЬ СТИЛЬ.
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
            var statusArray = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].GetStatusArray();
            List<string?> statuses = Cells.Select(c => c.Layer.LayerInfo.Status).Distinct().OrderBy(s => Array.IndexOf(statusArray, s)).ToList();
            _columns = statuses.Count;
            //statuses.Sort();
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


    }
}
