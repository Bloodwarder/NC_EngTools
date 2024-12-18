using System.Windows.Controls;

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
        public string Prefix { get; set; } = null!;

        public string MainName { get; set; } = null!;
        public string Separator { get; set; } = "_";
        public string Name
        {
            get
            {
                return string.Join(Separator, Prefix, MainName);
            }
            set
            {
                string[] classifiers = value.Split(Separator);
                Prefix = classifiers[0];
                MainName = string.Join(Separator, classifiers.Skip(1).Take(classifiers.Length - 1).ToArray());
            }
        }
        public LayerLegendData LayerLegendData { get; set; } = null!;

        //public int? AlternateLayerId { get; set; }
        public string? AlternateLayer { get; set; }

        public List<LayerData> Layers { get; } = new();

    }
}
