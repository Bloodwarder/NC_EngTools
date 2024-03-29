﻿using System;
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


        internal List<LegendGridRow> Rows = new();
        internal List<LegendGridCell> Cells = new();
        internal double Width { get => _columns * CellWidth + _columns * WidthInterval + TextWidth; }
        internal Point3d BasePoint = new();
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
            if (cell.ParentRow!.LegendData.IgnoreLayer)
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
            Rows = Rows.Where(r => !r.LegendData.IgnoreLayer).ToList();
            Rows.Sort();
            // Выбрать разделы и вставить их названия в нужные места таблицы
            var labels = Rows.Select(r => r.LegendEntityChapterName).Distinct().ToList();
            foreach (var label in labels)
            {
                LegendGridRow? row = new()
                {
                    ParentGrid = this,
                    Label = LegendInfoTable.Dictionary[label!],
                    ItalicLabel = true
                };
                Rows.Insert(Rows.IndexOf(Rows.Where(r => r.LegendEntityChapterName == label).Min()!), row);
            }
            // И то же самое для подразделов
            var sublabels = Rows.Select(r => r.LegendData.SubLabel).Where(s => s != null).Distinct().ToList();
            foreach (var label in sublabels)
            {
                LegendGridRow? row = new()
                {
                    ParentGrid = this,
                    Label = label
                };
                var labeledRows = Rows.Where(r => r.LegendData.SubLabel == label).ToList();
                int minindex = Rows.IndexOf(labeledRows.Min()!);
                int maxindex = Rows.IndexOf(labeledRows.Max()!);
                for (int i = minindex; i < maxindex; i++)
                {
                    Rows[i].IsSublabeledList = true;
                }
                Rows.Insert(minindex, row);

            }
            LegendGridRow? gridLabel = new()
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
            List<Status>? statuses = Cells.Select(c => c.Layer.BuildStatus).Distinct().ToList();
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
                    LegendGridRow? row = new(mainname)
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

        const double SEPARATED_GRIDS_OFFSET = 150d;
        internal List<LegendGridCell> SourceCells { get; set; } = new List<LegendGridCell>();
        internal List<LegendGrid> Grids { get; set; } = new List<LegendGrid>();

        private readonly TableFilter? _filter;
        Point3d _basepoint;
        internal GridsComposer(IEnumerable<LegendGridCell> cells, TableFilter filter)
        {
            SourceCells.AddRange(cells);
            _filter = filter;
        }

        // Компоновка сеток с условными обозначениями на основе выбранных фильтров
        internal void Compose(Point3d basepoint)
        {
            _basepoint = basepoint;
            switch (_filter)
            {
                case TableFilter.ExistingOnly:
                    // Сетка только с сущом
                    AddGrid(c => c.Layer.BuildStatus == Status.Existing);
                    break;

                case TableFilter.InternalOnly:
                    // Сетка с сущом, проектом и утверждаемым демонтажом
                    AddGrid(c => new Status[] { Status.Existing, Status.Deconstructing, Status.Planned }.Contains(c.Layer.BuildStatus));
                    break;

                case TableFilter.InternalAndExternal:
                    // Сетка с сущом, проектом и утверждаемым демонтажом
                    AddGrid(c => new Status[] { Status.Existing, Status.Deconstructing, Status.Planned }.Contains(c.Layer.BuildStatus));
                    // Сетка с неутв и неутв демонтажом
                    AddGrid(c => new Status[] { Status.NSDeconstructing, Status.NSPlanned }.Contains(c.Layer.BuildStatus));

                    break;

                case TableFilter.InternalAndSeparatedExternal:
                    // Сетка с сущом и проектом
                    AddGrid(c => new Status[] { Status.Existing, Status.Deconstructing, Status.Planned }.Contains(c.Layer.BuildStatus));
                    // Сетка с неутв и неутв демонтажом без метки конкретного внешнего проекта
                    AddGrid(c => new Status[] { Status.NSDeconstructing, Status.NSPlanned }.Contains(c.Layer.BuildStatus) && c.Layer.ExtProjectName == null);
                    // Сетки с неутв и неутв демонтажом для кадого конкретного внешнего проекта
                    List<string> extprojects = SourceCells.Where(c => c.Layer.ExtProjectName != null).Select(c => c.Layer.ExtProjectName).Distinct().ToList()!;
                    foreach (string extproject in extprojects)
                        AddGrid(c => c.Layer.ExtProjectName == extproject);
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
            List<LegendGridCell?> cells = new();
            foreach (LegendGridCell cell in filteredcells)
            {
                cells.Add(cell.Clone() as LegendGridCell);
            }
            // Посчитать точку вставки на основании уже вставленных сеток
            double deltax = Grids.Select(g => g.Width).Sum() + SEPARATED_GRIDS_OFFSET * Grids.Count;
            // Собрать сетку и добавить в список созданных сеток
            LegendGrid? grid = new(cells!, new Point3d(_basepoint.X + deltax, _basepoint.Y, 0d));
            grid.Assemble();
            Grids.Add(grid);
        }

        /// <summary>
        /// Создать и получить все объекты чертежа для отрисовки (вставки в ModelSpace) сеток условных обозначений
        /// </summary>
        /// <returns></returns>
        internal List<Entity> DrawGrids()
        {
            List<Entity>? entities = new();
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
        internal string? LegendEntityClassName { get; private set; }
        internal string? LegendEntityChapterName { get; private set; }
        internal LegendData LegendData { get; private set; }
        internal string Label { get => _islabelrow ? _label! : LegendData.Label; set => _label = value; }
        /// <summary>
        /// Помечает, что заголовок легенды является перечислением и в конце нужно поставить ";"
        /// </summary>
        internal bool IsSublabeledList { get; set; } = false;
        internal int YIndex;
        internal bool ItalicLabel = false;
        private readonly bool _islabelrow = false;
        private string? _label;
        private ObjectDraw? _draw;
        internal List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        internal LegendGrid? ParentGrid { get; set; }
        
        internal LegendGridRow()
        {
            _islabelrow = true;
        }
        internal LegendGridRow(LegendGridCell cell)
        {
            LegendEntityClassName = cell.Layer.MainName;
            LegendEntityChapterName = cell.Layer.EngType!;
        }
        internal LegendGridRow(string mainname)
        {
            LegendEntityClassName = mainname;
            LegendData = LayerLegendDictionary.GetValue(mainname, out bool success);
            if (!success)
                throw new System.Exception($"Нет данных для слоя {string.Concat(LayerParser.StandartPrefix, mainname)}");
        }


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
            string? label = _islabelrow ? _label : IsSublabeledList ? string.Concat(LegendData.Label, ";") : LegendData.Label; //переписать эту лесенку
            _draw = new LabelTextDraw(
                new Point2d(
                ParentGrid!.BasePoint.X + (ParentGrid.Width - LegendGrid.TextWidth) + LegendGrid.WidthInterval,
                ParentGrid.BasePoint.Y - YIndex * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2),
                label!,
                ItalicLabel);
            List<Entity>? list = new();
            _draw.Draw();
            list.AddRange(_draw.EntitiesList);
            return list;
        }

        public int CompareTo(object? obj)
        {
            LegendGridRow? lgr = obj as LegendGridRow;
            return this.LegendData.Rank.CompareTo(lgr!.LegendData.Rank);
        }
    }


    internal class LegendGridCell : ICloneable
    {
        List<LegendObjectDraw> _draw = new();
        private LegendDrawTemplate _template;

        internal LegendGridCell(RecordLayerParser layer)
        {
            Layer = layer;
            _template = LayerLegendDrawDictionary.GetValue(layer.TrueName, out _);
        }

        internal LegendGrid? ParentGrid { get; set; }
        internal LegendGridRow? ParentRow { get; set; }
        internal RecordLayerParser Layer { get; set; }
        internal CellTableIndex TableIndex = new();

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
            LegendObjectDraw? lod = Activator.CreateInstance
                (
                assemblyName: Assembly.GetCallingAssembly().FullName!,
                typeName: string.Concat("ModelspaceDraw.", _template.DrawTemplate, "Draw")
                )!
                .Unwrap() as LegendObjectDraw;

            lod!.LegendDrawTemplate = _template;
            lod.Layer = Layer;
            double x = ParentGrid!.BasePoint.X + TableIndex.X * (LegendGrid.CellWidth + LegendGrid.WidthInterval) + LegendGrid.CellWidth / 2;
            double y = ParentGrid.BasePoint.Y - TableIndex.Y * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2;
            lod.Basepoint = new Point2d(x, y);
            _draw.Add(lod);
        }

        public List<Entity> Draw()
        {
            List<Entity>? list = new();
            foreach (LegendObjectDraw lod in _draw)
            {
                lod.Draw();
                list.AddRange(lod.EntitiesList);
            }
            return list;
        }
        public object Clone()
        {
            LegendGridCell? lgc = (LegendGridCell)MemberwiseClone();
            lgc._draw = new List<LegendObjectDraw>();
            lgc.TableIndex = new CellTableIndex();
            lgc.ParentRow = null;
            return lgc;
        }
    }

    internal class LegendGridRowComparer : IComparer<LegendGridRow>
    {
        public int Compare(LegendGridRow? x, LegendGridRow? y)
        {
            return x!.LegendData.Rank.CompareTo(y!.LegendData.Rank);
        }
    }



    /// <summary>
    /// Словарь с именами разделов
    /// </summary>
    public static class LegendInfoTable
    {
        internal static Dictionary<string, string> Dictionary = new();

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

    /// <summary>
    /// Целочисленные индексы элементов для отрисовки легенды
    /// </summary>
    public struct CellTableIndex
    {
        /// <summary>
        /// Целочисленный индекс X
        /// </summary>
        public int X;
        /// <summary>
        /// Целочисленный индекс Y
        /// </summary>
        public int Y;
    }

    /// <summary>
    /// Данные слоя для компоновки легенды
    /// </summary>
    public struct LegendData
    {
        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank;
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label;
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string SubLabel;
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer;
    }
    /// <summary>
    /// Фильтр для компоновщика сетки легенды
    /// </summary>
    public enum TableFilter
    {
        /// <summary>
        /// Только существующие
        /// </summary>
        ExistingOnly,
        /// <summary>
        /// Полная таблица
        /// </summary>
        Full,
        /// <summary>
        /// Только существующие и утверждаемые проектные / демонтируемые сети
        /// </summary>
        InternalOnly,
        /// <summary>
        /// Две отдельных таблицы с утверждаемыми и не утверждаемыми сетями
        /// </summary>
        InternalAndExternal,
        /// <summary>
        /// Таблица для утверждаемых объектов и отдельные таблицы для каждого имени внешнего проекта
        /// </summary>
        InternalAndSeparatedExternal
    }
}
