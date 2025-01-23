
namespace LoaderCore.SharedData
{
    public class ZoneInfo
    {
        public ZoneInfo(string zoneLayer, double value, double defaultWidth, string filter, bool ignoreDefaultWidth)
        {
            ZoneLayer = zoneLayer;
            Value = value;
            DefaultConstructionWidth = defaultWidth;
            AdditionalFilter = filter;
            IgnoreConstructionWidth = ignoreDefaultWidth;
        }

        public string ZoneLayer { get; set; }
        public string? AdditionalFilter { get; set; }
        public bool IgnoreConstructionWidth { get; set; }

        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
    }
}
