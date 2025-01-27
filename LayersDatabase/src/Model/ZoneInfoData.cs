namespace LayersIO.Model
{
    public class ZoneInfoData
    {
#nullable disable warnings
        public ZoneInfoData() { }
#nullable restore
        public int Id { get; set; }
        public LayerData SourceLayer { get; set; }
        public int SourceLayerId { get; set; }
        public LayerData ZoneLayer { get; set; }
        public int ZoneLayerId { get; set; }
        public string? AdditionalFilter { get; set; }
        public bool IgnoreConstructionWidth { get; set; } = true;
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
        public bool IsSpecial { get; set; } = false;
    }
}
