
using System.Windows.Controls;

namespace LayersIO.Model
{
    public class LayerData
    {
        public int Id { get; set; }
        public string Separator = "_";
        public string MainName { get; set; }
        public string StatusName { get; set; }

        public LayerGroupData LayerGroup { get; set; }
        public string Name
        {
            get
            {
                return string.Concat(MainName,"_",StatusName);
            }
            set
            {
                string[] classifiers = value.Split('_');
                MainName = string.Join("_", classifiers.Take(classifiers.Length-1).ToArray());
                StatusName = classifiers[^1];
            }
        }
        public LayerPropertiesData LayerPropertiesData { get; set; }
        public LayerDrawTemplateData LayerDrawTemplateData { get; set; }
    }
}
