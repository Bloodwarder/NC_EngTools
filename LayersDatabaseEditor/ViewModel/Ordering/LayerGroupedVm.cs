using LayersIO.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class LayerGroupedVm : IDbRelatedViewModel, INotifyPropertyChanged
    {
        private readonly LayerData _layer;
        private bool _includeState;
        private DrawOrderGroupVm? _initialGroup;

        public LayerGroupedVm(LayerData layer)
        {
            _layer = layer;
            Name = layer.Name;
            ResetValues();
        }

        public string Name { get; }

        public bool IsValid => true;

        public bool IsUpdated => Group != _initialGroup;

        internal DrawOrderGroupVm? Group { get; set; }

        public bool IncludeState
        {
            get => _includeState;
            set
            {
                _includeState = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ResetValues()
        {
            if (_layer.DrawOrderGroup != null && DrawOrderGroupVm.NameMap.ContainsKey(_layer.DrawOrderGroup.Name))
            {
                Group = DrawOrderGroupVm.NameMap[_layer.DrawOrderGroup.Name];
            }
            else
            {
                Group = null;
            }
            _initialGroup = Group;
        }

        public void UpdateDatabaseEntities()
        {
            _layer.DrawOrderGroup = Group?.DrawOrderGroup;
            _initialGroup = Group;
        }

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

    }
}
