
namespace LayersIO.Model
{
    public class LayerData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LayerPropertiesData LayerPropertiesData { get; set; }
        public LayerDrawTemplateData LayerDrawTemplateData { get; set; }
    }
}
