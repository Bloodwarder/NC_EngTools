using LayersIO.Connection;
using LayersIO.Model;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerPropertiesVm : INotifyPropertyChanged
    {
        private readonly LayerPropertiesData _layerPropertiesData;
        private readonly LayersDatabaseContextSqlite _db;

        private Color _color;
        private string? _linetypeName;
        private double _linetypeScale;
        private double _constantWidth;
        private int _lineWeight;
        private int _drawOrderIndex;

        static LayerPropertiesVm()
        {
            
        }
        public LayerPropertiesVm(LayerPropertiesData layerPropertiesData, LayersDatabaseContextSqlite context)
        {
            _db = context;
            _layerPropertiesData = layerPropertiesData;
            ResetValues();
        }

        internal LayersDatabaseContextSqlite Database => _db;

        /// <summary>
        /// Глобальная ширина
        /// </summary>
        public double ConstantWidth
        {
            get => _constantWidth;
            set
            {
                _constantWidth = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        public double LinetypeScale
        {
            get => _linetypeScale;
            set
            {
                _linetypeScale = value;
                OnPropertyChanged();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Тип линий
        /// </summary>
        public string? LinetypeName
        {
            get => _linetypeName;
            set
            {
                _linetypeName = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Вес линий
        /// </summary>
        public int LineWeight
        {
            get => _lineWeight;
            set
            {
                _lineWeight = value;
                OnPropertyChanged();
            }
        }

        public int DrawOrderIndex
        {
            get => _drawOrderIndex;
            set
            {
                _drawOrderIndex = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateDbEntity()
        {
            // Валидация в родительском объекте

            _layerPropertiesData.ConstantWidth = ConstantWidth;
            _layerPropertiesData.LinetypeScale = LinetypeScale;
            _layerPropertiesData.LinetypeName = LinetypeName!;
            _layerPropertiesData.LineWeight = LineWeight;
            _layerPropertiesData.Red = this.Color.R;
            _layerPropertiesData.Green = this.Color.G;
            _layerPropertiesData.Blue = this.Color.B;
            _layerPropertiesData.DrawOrderIndex = DrawOrderIndex;
        }

        public void ResetValues()
        {
            ConstantWidth = _layerPropertiesData.ConstantWidth;
            LinetypeScale = _layerPropertiesData.LinetypeScale;
            Color = Color.FromRgb(_layerPropertiesData.Red, _layerPropertiesData.Green, _layerPropertiesData.Blue);
            LinetypeName = _layerPropertiesData.LinetypeName;
            LineWeight = _layerPropertiesData.LineWeight;
            DrawOrderIndex = _layerPropertiesData.DrawOrderIndex;
        }

        public bool IsUpdated()
        {
            bool isUpdated =
            ConstantWidth != _layerPropertiesData.ConstantWidth
            || LinetypeScale != _layerPropertiesData.LinetypeScale
            || Color != Color.FromRgb(_layerPropertiesData.Red, _layerPropertiesData.Green, _layerPropertiesData.Blue)
            || LinetypeName != _layerPropertiesData.LinetypeName
            || LineWeight != _layerPropertiesData.LineWeight
            || DrawOrderIndex != _layerPropertiesData.DrawOrderIndex;
            return isUpdated;
        }

        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}