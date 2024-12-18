using LayersIO.Connection;
using LayersIO.Model;
using System.Collections.ObjectModel;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerGroupViewModel
    {
        private readonly LayerGroupData _layerGroupData;
        private readonly LayersDatabaseContextSqlite _db;

        public LayerGroupViewModel(LayerGroupData layerGroupData, LayersDatabaseContextSqlite context)
        {
            _db = context;
            _layerGroupData = layerGroupData;
            Prefix = layerGroupData.Prefix;
            MainName = layerGroupData.MainName;
            Separator = layerGroupData.Separator;
            foreach(var layer in layerGroupData.Layers)
                Layers.Add(new LayerDataViewModel(layer, _db));
        }


        public string? Prefix { get; set; }

        public string? MainName { get; set; }

        public string Separator { get; set; }
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

        public string? AlternateLayer { get; set; }


        public ObservableCollection<LayerDataViewModel> Layers { get; } = new();

        internal void SaveChanges()
        {
            // TODO: VALIDATE

            _layerGroupData.Prefix = Prefix;
            _layerGroupData.AlternateLayer = AlternateLayer;
        }
    }
}