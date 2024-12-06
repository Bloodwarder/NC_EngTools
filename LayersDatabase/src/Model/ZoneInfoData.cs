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
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
    }
}
