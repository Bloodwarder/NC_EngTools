using LayersIO.Connection;
using LayersIO.Model;

namespace LayersDatabaseEditor.ViewModel
{
    public class ZoneInfoViewModel
    {
        private readonly ZoneInfoData _zoneInfoData;
        private readonly LayersDatabaseContextSqlite _db;
        public ZoneInfoViewModel(ZoneInfoData zoneInfoData, LayersDatabaseContextSqlite context)
        {
            _db = context;
            _zoneInfoData = zoneInfoData;
            ZoneLayer = zoneInfoData.ZoneLayer.Name;
            AdditionalFilter = zoneInfoData.AdditionalFilter;
            IgnoreConstructionWidth = zoneInfoData.IgnoreConstructionWidth;
            DefaultConstructionWidth = zoneInfoData.DefaultConstructionWidth;
            Value = zoneInfoData.Value;
        }

        public string? ZoneLayer { get; set; }
        public string? AdditionalFilter { get; set; }
        public bool IgnoreConstructionWidth { get; set; }
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }

        internal void SaveChanges()
        {
            // TODO: VALIDATE

            _zoneInfoData.Value = Value;
            _zoneInfoData.DefaultConstructionWidth = DefaultConstructionWidth;
            _zoneInfoData.IgnoreConstructionWidth = IgnoreConstructionWidth;
            _zoneInfoData.AdditionalFilter = AdditionalFilter;
        }
    }
}