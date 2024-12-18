using LayersIO.Model;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerPropertiesViewModel : INotifyPropertyChanged
    {
        private readonly LayerPropertiesData _layerPropertiesData;
        private Color _color;

        static LayerPropertiesViewModel()
        {

        }
        public LayerPropertiesViewModel(LayerPropertiesData layerPropertiesData)
        {
            _layerPropertiesData = layerPropertiesData;
            ConstantWidth = layerPropertiesData.ConstantWidth;
            LinetypeScale = layerPropertiesData.LinetypeScale;
            Color = Color.FromRgb(layerPropertiesData.Red, layerPropertiesData.Green, layerPropertiesData.Blue);
            LinetypeName = layerPropertiesData.LinetypeName;
            LineWeight = layerPropertiesData.LineWeight;
        }

        /// <summary>
        /// Глобальная ширина
        /// </summary>
        public double ConstantWidth { get; set; }
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        public double LinetypeScale { get; set; }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }
        /// <summary>
        /// Тип линий
        /// </summary>
        public string LinetypeName { get; set; }
        /// <summary>
        /// Вес линий
        /// </summary>
        public int LineWeight { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        internal void SaveChanges()
        {
            // VALIDATE

            _layerPropertiesData.ConstantWidth = ConstantWidth;
            _layerPropertiesData.LinetypeScale = LinetypeScale;
            _layerPropertiesData.LinetypeName = LinetypeName;
            _layerPropertiesData.LineWeight = LineWeight;
            _layerPropertiesData.Red = this.Color.R;
            _layerPropertiesData.Green = this.Color.G;
            _layerPropertiesData.Blue = this.Color.B;
        }
    }
}