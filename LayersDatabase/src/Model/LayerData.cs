namespace LayersIO.Model
{
    /// <summary>
    /// Model containing data for layer with exact status
    /// </summary>
    /// <exception cref="NullReferenceException">Required members are null</exception>
    public class LayerData
    {
        public LayerData() { }
        public LayerData(string name)
        {
            Name = name;
        }


        public int Id { get; set; }
        public string Separator = "_"; // TODO: Заменить на универсальную конструкцию. Брать из xml или из сборки LayerWorks. Или устанавливать из LayersDatabaseEditor
        public string MainName { get; set; } = null!;
        public string StatusName { get; set; } = null!;

        public LayerGroupData LayerGroup { get; set; } = null!;
        public string Name
        {
            get
            {
                return string.Concat(MainName, Separator, StatusName);
            }
            set
            {
                string[] classifiers = value.Split(Separator);
                MainName = string.Join(Separator, classifiers.Take(classifiers.Length - 1).ToArray());
                StatusName = classifiers[^1];
            }
        }
        public LayerPropertiesData? LayerPropertiesData { get; set; }
        public LayerDrawTemplateData? LayerDrawTemplateData { get; set; }
        public List<ZoneInfo> Zones { get; set; }

        public bool IsEmpty => LayerPropertiesData is null || LayerDrawTemplateData is null || MainName is null;
    }
}
