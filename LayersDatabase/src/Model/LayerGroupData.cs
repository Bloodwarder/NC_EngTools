namespace LayersIO.Model
{
    public class LayerGroupData
    {
        public LayerGroupData() { }

        public LayerGroupData(string mainName)
        {
            MainName = mainName;
        }

        public int Id { get; set; }

        public string MainName { get; set; } = null!;

        public LayerLegendData LayerLegendData { get; set; } = null!;

        //public int? AlternateLayerId { get; set; }
        public string? AlternateLayer { get; set; }

        public List<LayerData> Layers { get; } = new();

    }
}
