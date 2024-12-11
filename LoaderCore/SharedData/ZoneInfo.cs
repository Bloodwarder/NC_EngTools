
namespace LoaderCore.SharedData
{
    public class ZoneInfo
    {
        public ZoneInfo(string zoneLayer, double value, double defaultWidth)
        {
            ZoneLayer = zoneLayer;
            Value = value;
            DefaultConstructionWidth = defaultWidth;
        }

        public string ZoneLayer { get; set; }
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
    }
}
