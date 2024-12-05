using LayersIO.Model;

namespace LayersIO.DataTransfer
{
    public class ZoneInfo
    {
        public ZoneInfo(string zoneLayer, double value, double defaultWidth)
        {
            ZoneLayer = zoneLayer;
            Value = value;
            DefaultConstructionWidth = defaultWidth;
        }

        public ZoneInfo(ZoneInfoData zoneInfoData)
        {
            ZoneLayer = zoneInfoData.ZoneLayer.Name;
            DefaultConstructionWidth = zoneInfoData.DefaultConstructionWidth;
            Value = zoneInfoData.Value;
        }
        public string ZoneLayer { get; set; }
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
    }
}
