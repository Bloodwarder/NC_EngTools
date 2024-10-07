using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.ModelspaceDraw;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGridRow : IComparable
    {
        internal string? LegendEntityClassName { get; private set; }
        internal string? LegendEntityChapterName { get; private set; }
        internal LegendData? LegendData { get; private set; }
        internal string? Label { get => _islabelrow ? _label : LegendData?.Label; set => _label = value; }
        /// <summary>
        /// Помечает, что заголовок легенды является перечислением и в конце нужно поставить ";"
        /// </summary>
        internal bool IsSublabeledList { get; set; } = false;
        internal int YIndex;
        internal bool ItalicLabel = false;
        private readonly bool _islabelrow = false;
        private string? _label;
        private ObjectDraw? _draw;

        internal LegendGridRow()
        {
            _islabelrow = true;
        }
        internal LegendGridRow(LegendGridCell cell)
        {
            LegendEntityClassName = cell.Layer.LayerInfo.MainName;
            LegendEntityChapterName = cell.Layer.LayerInfo.PrimaryClassifier;
        }
        internal LegendGridRow(string mainname)
        {
            LegendEntityClassName = mainname;

            var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendData>>();
            bool success = service.TryGet(mainname, out LegendData? ld);
            if (success)
                LegendData = ld;
            else
                throw new Exception($"Нет данных для слоя {string.Concat(LayerWrapper.StandartPrefix, mainname)}");

        }
        internal List<LegendGridCell> Cells { get; set; } = new List<LegendGridCell>();
        internal LegendGrid? ParentGrid { get; set; }

        public void AddCell(LegendGridCell legendGridCell)
        {
            Cells.Add(legendGridCell);
            legendGridCell.ParentRow = this;
            if (LegendEntityChapterName != null)
                return;
            LegendEntityChapterName = legendGridCell.Layer.LayerInfo.PrimaryClassifier;
        }

        internal void AssignY(int y)
        {
            YIndex = y;
            foreach (LegendGridCell cell in Cells)
                cell.AssignY(y);
        }
        public List<Entity> Draw()
        {
            string? label = _islabelrow ? _label : (IsSublabeledList ? string.Concat(LegendData?.Label, ";") : LegendData?.Label); //переписать эту лесенку
            double xCoord = ParentGrid!.BasePoint.X + (ParentGrid.Width - LegendGrid.TextWidth) + LegendGrid.WidthInterval;
            double yCoord = ParentGrid.BasePoint.Y - YIndex * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2;
            Point2d point = new(xCoord, yCoord);
            try
            {
                _draw = new LabelTextDraw(point, label ?? "ОШИБКА. НЕ ПОЛУЧЕНЫ ДАННЫЕ ПОДПИСИ", ItalicLabel);
            }
            catch (Exception ex)
            {
                // UNDONE: блок добавлен, чтобы ловить во время отладки. Пересмотреть.
                throw;
            }

            List<Entity> list = new List<Entity>();
            _draw.Draw();
            list.AddRange(_draw.EntitiesList);
            return list;
        }

        public int CompareTo(object? obj)
        {
            LegendGridRow? lgr = obj as LegendGridRow;
            if (lgr == null)
                return -1;
            int rankThis = LegendData?.Rank ?? int.MaxValue;
            int rankCompared = lgr.LegendData?.Rank ?? int.MaxValue;
            return rankThis.CompareTo(rankCompared);
        }
    }
}
