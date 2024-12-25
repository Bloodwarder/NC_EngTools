namespace LayersIO.Model
{
    /// <summary>
    /// Model containing data for layer with exact status
    /// </summary>
    /// <exception cref="NullReferenceException">Required members are null</exception>
    public class LayerData
    {
        public LayerData() { }
        public LayerData(LayerGroupData layerGroup)
        {
            LayerGroup = layerGroup;
        }
        public LayerData(LayerGroupData layerGroup, string status) : this(layerGroup)
        {
            StatusName = status;
        }


        public int Id { get; set; }

        public string Prefix => LayerGroup.Prefix;

        public string Separator = "_"; // TODO: Заменить на универсальную конструкцию. Брать из xml или из сборки LayerWorks. Или устанавливать из LayersDatabaseEditor
        public string MainName => LayerGroup.MainName;
        public string? StatusName { get; set; }

        public int LayerGroupId { get; set; }
        public LayerGroupData LayerGroup { get; set; }
        public string Name => string.Join(Separator, Prefix, MainName, StatusName);

        public LayerPropertiesData LayerPropertiesData { get; set; } = new();
        public LayerDrawTemplateData LayerDrawTemplateData { get; set; } = new();
        public List<ZoneInfoData> Zones { get; set; } = new();

        public bool IsEmpty => LayerPropertiesData is null || LayerDrawTemplateData is null || MainName is null;
    }
}
