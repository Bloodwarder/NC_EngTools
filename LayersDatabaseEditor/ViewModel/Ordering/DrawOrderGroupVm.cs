using LayersIO.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class DrawOrderGroupVm : IDbRelatedViewModel, INotifyPropertyChanged
    {
        private const string NewGroupName = "Новая группа";
        private readonly DrawOrderGroup _drawOrderGroup;
        private string _name = null!;
        private int _index;
        private bool _isMapped = false;

        public DrawOrderGroupVm(DrawOrderGroup drawOrderGroup)
        {
            _drawOrderGroup = drawOrderGroup;
            ResetValues();
        }
        public DrawOrderGroupVm()
        {
            _drawOrderGroup = new()
            {
                Index = 0,
                Name = NewGroupName
            };
            ResetValues();
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != null && NameMap.ContainsKey(_name))
                    NameMap.Remove(_name);
                _isMapped = NameMap.TryAdd(value, this);
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    int oldValue = _index;
                    _index = value;

                    PropertyChanged?.Invoke(this, new IndexChangedEventArgs(oldValue, value));
                    OnPropertyChanged(nameof(IsValid));
                    OnPropertyChanged(nameof(IsUpdated));

                    //if (Math.Abs(oldValue - value) > 1)
                    //    OnIndexLeap?.Invoke(this, new(oldValue, value));
                }
            }
        }

        public bool IsUpdated => Name != _drawOrderGroup.Name || Index != _drawOrderGroup.Index;
        public bool IsValid => !string.IsNullOrEmpty(Name) && Name != NewGroupName && _isMapped;

        internal static Dictionary<string, DrawOrderGroupVm> NameMap { get; } = new();
        internal DrawOrderGroup DrawOrderGroup => _drawOrderGroup;

        public event PropertyChangedEventHandler? PropertyChanged;
        //internal event IndexLeapEventHandler? OnIndexLeap;

        public void ResetValues()
        {
            if (Name != null && NameMap.ContainsKey(Name))
                NameMap.Remove(Name);
            _isMapped = NameMap.TryAdd(_drawOrderGroup.Name, this);

            Name = _drawOrderGroup.Name;
            Index = _drawOrderGroup.Index;
        }

        public void UpdateDatabaseEntities()
        {
            _drawOrderGroup.Name = Name;
            _drawOrderGroup.Index = Index;
        }

        internal void SetIndexDeferRecalc(int newIndex)
        {
            _index = newIndex;
            OnPropertyChanged(nameof(Index));
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(IsUpdated));
        }


        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        public class IndexChangedEventArgs : PropertyChangedEventArgs
        {
            public IndexChangedEventArgs(int oldIndex, int newIndex) : base(nameof(Index))
            {
                OldIndex = oldIndex;
                NewIndex = newIndex;
            }
            public int OldIndex { get; }
            public int NewIndex { get; }
        }

        //public delegate void IndexLeapEventHandler(object sender, IndexLeapEventArgs e);

    }
}
