using NameClassifiers;
using NameClassifiers.Filters;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class GridsComposer : IDisposable
    {

        const double SeparatedGridsOffset = 150d;
        internal static GridsComposer? ActiveComposer { get; private set; }
        internal List<LegendGridCell> SourceCells { get; set; } = new List<LegendGridCell>();
        internal List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();

        private readonly string[] _keywords;
        Point3d _basepoint;
        internal GridsComposer(IEnumerable<LegendGridCell> cells, string[] keywords)
        {
            ActiveComposer = this;
            SourceCells.AddRange(cells);
            _keywords = keywords;
        }

        // Компоновка сеток с условными обозначениями на основе выбранных фильтров
        internal void Compose(Point3d basepoint)
        {
            _basepoint = basepoint;
            GlobalFilters globalFilters = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].GlobalFilters;
            IEnumerable<GridData> grids = globalFilters.GetGridData(_keywords);
            foreach (var grid in grids)
                AddGrid(grid);
        }
        private void AddGrid(GridData gridData)
        {
            // Отфильтровать ячейки, созданные для успешно обработанных слоёв и клонировать их в новый список в соответствии с заданным фильтром
            List<LegendGridCell> filteredcells = SourceCells.Where(gridData.Predicate).ToList();
            List<LegendGridCell> cells = new();
            foreach (LegendGridCell cell in filteredcells)
            {
                cells.Add((LegendGridCell)cell.Clone());
            }
            // Посчитать точку вставки на основании уже вставленных сеток
            double deltax = Grids.Select(g => g.Width).Sum() + SeparatedGridsOffset * Grids.Count;
            // Собрать сетку и добавить в список созданных сеток
            LegendGrid grid = new(cells, new Point3d(_basepoint.X + deltax, _basepoint.Y, 0d), gridData.GridName);
            grid.Assemble();
            Grids.Add(grid);
        }

        /// <summary>
        /// Создать и получить все объекты чертежа для отрисовки (вставки в ModelSpace) сеток условных обозначений
        /// </summary>
        /// <returns></returns>
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

        public void Dispose()
        {
            ActiveComposer = null;
            GC.SuppressFinalize(this);
        }
    }
}
