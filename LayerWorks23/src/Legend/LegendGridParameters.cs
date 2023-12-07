namespace LayerWorks.Legend
{
    internal class LegendGridParameters
    {
        private const double _defaultCellWidth = 30;
        private const double _defaultCellHeight = 9;
        private const double _defaultWidthInterval = 5;
        private const double _defaultHeightInterval = 5;
        private const double _defaultTextWidth = 150;
        private const double _defaultTextHeight = 4;

        internal double CellWidth { get; set; } = _defaultCellWidth;
        internal double CellHeight { get; set; } = _defaultCellHeight;
        internal double WidthInterval { get; set; } = _defaultWidthInterval;
        internal double HeightInterval { get; set; } = _defaultHeightInterval;
        internal double TextWidth { get; set; } = _defaultTextWidth;
        internal double TextHeight { get; set; } = _defaultTextHeight;
    }
}