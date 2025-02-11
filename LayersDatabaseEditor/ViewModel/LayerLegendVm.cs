using LayersDatabaseEditor.ViewModel.Validation;
using LayersIO.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerLegendVm : INotifyPropertyChanged
    {
        private readonly LayerLegendData _layerLegendData;
        private int _rank;
        private string? _label;
        private string? _subLabel;
        private bool _ignoreLayer;

        public LayerLegendVm(LayerLegendData layerLegendData)
        {
            _layerLegendData = layerLegendData;
            ResetValues();
        }

        public static LayerLegendViewModelValidator Validator { get; private set; } = new();

        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank
        {
            get => _rank;
            set
            {
                _rank = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string? Label
        {
            get => _label;
            set
            {
                _label = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string? SubLabel
        {
            get => _subLabel;
            set
            {
                _subLabel = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer
        {
            get => _ignoreLayer;
            set
            {
                _ignoreLayer = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void ResetValues()
        {
            Rank = _layerLegendData.Rank;
            Label = _layerLegendData.Label;
            SubLabel = _layerLegendData.SubLabel;
            IgnoreLayer = _layerLegendData.IgnoreLayer;
        }

        internal void UpdateDbEntity()
        {
            // Валидация в родительском элементе LayerGroupViewModel

            _layerLegendData.Rank = Rank;
            _layerLegendData.Label = Label!; // не нулл после валидации
            _layerLegendData.SubLabel = SubLabel == string.Empty ? null : SubLabel;
            _layerLegendData.IgnoreLayer = IgnoreLayer;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsUpdated()
        {
            bool isUpdated = _rank != _layerLegendData.Rank ||
                            _label != _layerLegendData.Label ||
                            (_subLabel != _layerLegendData.SubLabel && !(_subLabel == "" && _layerLegendData.SubLabel == null)) ||
                            _ignoreLayer != _layerLegendData.IgnoreLayer;
            return isUpdated;
        }
    }
}