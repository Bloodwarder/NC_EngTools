using LayersIO.Model;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerLegendViewModel
    {
        private readonly LayerLegendData _layerLegendData;
        public LayerLegendViewModel(LayerLegendData layerLegendData)
        {
            _layerLegendData = layerLegendData;
            Rank = layerLegendData.Rank;
            Label = layerLegendData.Label;
            SubLabel = layerLegendData.SubLabel;
            IgnoreLayer = layerLegendData.IgnoreLayer;
        }

        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string? SubLabel { get; set; }
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer { get; set; } = false;

        internal void SaveChanges()
        {
            // TODO: VALIDATE

            _layerLegendData.Rank = Rank;
            _layerLegendData.Label = Label;
            _layerLegendData.SubLabel = SubLabel;
            _layerLegendData.IgnoreLayer = IgnoreLayer;
        }
    }
}