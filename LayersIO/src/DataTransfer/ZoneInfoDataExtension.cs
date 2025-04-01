using LayersIO.Model;
using LoaderCore.SharedData;

namespace LayersIO.DataTransfer
{
    public static class ZoneInfoDataExtension
    {
        public static ZoneInfo ToZoneInfo(this ZoneInfoData zoneInfoData)
        {
            ZoneInfo info = new(zoneInfoData.ZoneLayer.Name,
                                zoneInfoData.Value,
                                zoneInfoData.DefaultConstructionWidth,
                                zoneInfoData.AdditionalFilter,
                                zoneInfoData.IgnoreConstructionWidth,
                                zoneInfoData.IsSpecial);
            return info;
        }
    }
}
