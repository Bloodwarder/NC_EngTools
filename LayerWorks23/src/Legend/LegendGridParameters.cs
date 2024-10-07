using LoaderCore.Configuration;

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

        internal double CellWidth { get; set; }
        internal double CellHeight { get; set; }
        internal double WidthInterval { get; set; }
        internal double HeightInterval { get; set; }
        internal double TextWidth { get; set; }
        internal double TextHeight { get; set; }

        internal static LegendGridParameters GetDefault()
        {
            return new()
            {
                CellWidth = _defaultCellWidth,
                CellHeight = _defaultCellHeight,
                WidthInterval = _defaultWidthInterval,
                HeightInterval = _defaultHeightInterval,
                TextWidth = _defaultTextWidth,
                TextHeight = _defaultTextHeight
            };
        }
    }

    internal static class LayerWorksConfigurationExtension
    {
        internal static LegendGridParameters GetLegendGridParameters(this LayerWorksConfiguration config)
        {
            if (config.LegendGridParameters == null)
                return new LegendGridParameters();

            LegendGridParameters parameters = new()
            {
                CellHeight = config.LegendGridParameters.CellHeight,
                CellWidth = config.LegendGridParameters.CellWidth,
                HeightInterval = config.LegendGridParameters.HeightInterval,
                TextHeight = config.LegendGridParameters.TextHeight,
                TextWidth = config.LegendGridParameters.TextWidth,
                WidthInterval = config.LegendGridParameters.WidthInterval
            };
            return parameters;
        }
    }
}