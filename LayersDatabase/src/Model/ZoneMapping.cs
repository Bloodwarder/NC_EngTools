namespace LayersIO.Model
{
    public class ZoneMapping
    {
        public ZoneMapping()
        {
            
        }

        public int Id { get; set; }

        public string SourcePrefix { get; set; }

        public string SourceStatus { get; set; }

        public string? AdditionalFilter { get; set; }

        public string TargetPrefix { get; set; }

        public string TargetStatus { get; set; }
    }
}
