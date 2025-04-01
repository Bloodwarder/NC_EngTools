using LoaderCore.Configuration;

namespace LayerWorks.Legend
{
    public class LegendGridParameters
    {
        private const double _defaultCellWidth = 30d;
        private const double _defaultCellHeight = 9d;
        private const double _defaultWidthInterval = 5d;
        private const double _defaultHeightInterval = 5d;
        private const double _defaultTextWidth = 150d;
        private const double _defaultTextHeight = 4d;
        private const double _defaultMarkedLineTextHeight = 4d;

        public double CellWidth { get; set; }
        public double CellHeight { get; set; }
        public double WidthInterval { get; set; }
        public double HeightInterval { get; set; }
        public double TextWidth { get; set; }
        public double TextHeight { get; set; }
        public double MarkedLineTextHeight { get; set; }

        internal static LegendGridParameters GetDefault()
        {
            return new()
            {
                CellWidth = _defaultCellWidth,
                CellHeight = _defaultCellHeight,
                WidthInterval = _defaultWidthInterval,
                HeightInterval = _defaultHeightInterval,
                TextWidth = _defaultTextWidth,
                TextHeight = _defaultTextHeight,
                MarkedLineTextHeight = _defaultMarkedLineTextHeight
            };
        }
    }
}