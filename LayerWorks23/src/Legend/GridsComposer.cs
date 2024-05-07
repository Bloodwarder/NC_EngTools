using System;
using System.Collections.Generic;
using System.Linq;
using LayerWorks.LayerProcessing;
using NameClassifiers;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class GridsComposer
    {

        const double SeparatedGridsOffset = 150d;
        internal List<LegendGridCell> SourceCells { get; set; } = new List<LegendGridCell>();
        internal List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();

        private readonly TableFilter _filter;
        Point3d _basepoint;
        internal GridsComposer(IEnumerable<LegendGridCell> cells, TableFilter filter)
        {
            SourceCells.AddRange(cells);
            _filter = filter;
        }

        // Компоновка сеток с условными обозначениями на основе выбранных фильтров
        internal void Compose(Point3d basepoint)
        {
            // Починено под изначальный парсер чтобы компилировалось и работало
            // Сделать пользовательский выбор для конкретного парсера, данные взять из xml
            _basepoint = basepoint;
            switch (_filter)
            {
                case TableFilter.ExistingOnly:
                    // Сетка только с сущом
                    AddGrid(c => c.Layer.LayerInfo.Status == "сущ");
                    break;

                case TableFilter.InternalOnly:
                    // Сетка с сущом, проектом и утверждаемым демонтажом
                    AddGrid(c => new string[] { "сущ", "дем", "пр"}.Contains(c.Layer.LayerInfo.Status));
                    break;

                case TableFilter.InternalAndExternal:
                    // Сетка с сущом, проектом и утверждаемым демонтажом
                    AddGrid(c => new string[] { "сущ", "дем", "пр" }.Contains(c.Layer.LayerInfo.Status));
                    // Сетка с неутв и неутв демонтажом
                    AddGrid(c => new string[] { "ндем", "неутв"}.Contains(c.Layer.LayerInfo.Status));

                    break;

                case TableFilter.InternalAndSeparatedExternal:
                    // Сетка с сущом и проектом
                    AddGrid(c => new string[] { "сущ", "дем", "пр" }.Contains(c.Layer.LayerInfo.Status));
                    // Сетка с неутв и неутв демонтажом без метки конкретного внешнего проекта
                    AddGrid(c => new string[] { "ндем", "неутв" }.Contains(c.Layer.LayerInfo.Status) && c.Layer.LayerInfo.AuxilaryData["ExternalProject"] == null);
                    // Сетки с неутв и неутв демонтажом для кадого конкретного внешнего проекта
                    List<string?> extprojects = SourceCells
                        .Where(c => c.Layer.LayerInfo.AuxilaryData["ExternalProject"] != null)
                        .Select(c => c.Layer.LayerInfo.AuxilaryData["ExternalProject"])
                        .Distinct().ToList();
                    foreach (var extproject in extprojects)
                        AddGrid(c => c.Layer.LayerInfo.AuxilaryData["ExternalProject"] == extproject);
                    break;

                case TableFilter.Full:
                    // Полная общая сетка для всех успешно обработанных слоёв
                    AddGrid(c => true);
                    break;
            }

        }
        private void AddGrid(Func<LegendGridCell, bool> predicate)
        {
            // Отфильтровать ячейки, созданные для успешно обработанных слоёв и клонировать их в новый список в соответствии с заданным фильтром
            List<LegendGridCell> filteredcells = SourceCells.Where(predicate).ToList();
            List<LegendGridCell> cells = new List<LegendGridCell>();
            foreach (LegendGridCell cell in filteredcells)
            {
                cells.Add((LegendGridCell)cell.Clone());
            }
            // Посчитать точку вставки на основании уже вставленных сеток
            double deltax = Grids.Select(g => g.Width).Sum() + SeparatedGridsOffset * Grids.Count;
            // Собрать сетку и добавить в список созданных сеток
            LegendGrid grid = new LegendGrid(cells, new Point3d(_basepoint.X + deltax, _basepoint.Y, 0d));
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
    }
}
