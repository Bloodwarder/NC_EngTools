namespace LayersIO.Model
{
    /// <summary>
    /// Model containing data for layer with exact status
    /// </summary>
    /// <exception cref="NullReferenceException">Required members are null</exception>
    public class LayerData
    {
        public int Id { get; set; }
        public string Separator = "_"; // TODO: Заменить на универсальную конструкцию. Брать из xml или из сборки LayerWorks. Или устанавливать из LayersDatabaseEditor
        public string MainName { get; set; } = null!;
        public string StatusName { get; set; } = null!;

        public LayerGroupData LayerGroup { get; set; } = null!;
        public string Name
        {
            get
            {
                return string.Concat(MainName,Separator,StatusName);
            }
            set
            {
                string[] classifiers = value.Split(Separator);
                MainName = string.Join(Separator, classifiers.Take(classifiers.Length-1).ToArray());
                StatusName = classifiers[^1];
            }
        }
        public LayerPropertiesData LayerPropertiesData { get; set; } = null!;
        public LayerDrawTemplateData LayerDrawTemplateData { get; set; } = null!;
    }
}
