using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public abstract class OrderedItemViewModel : INotifyPropertyChanged, IDbRelatedViewModel
    {
        public abstract string Name { get; }
        public abstract int Index { get; set; }

        public abstract bool IsValid { get; }
        public abstract bool IsUpdated { get; }

        public abstract event PropertyChangedEventHandler? PropertyChanged;

        public abstract void ResetValues();
        public abstract void UpdateDatabaseEntities();
    }

    public class OrderedItemViewModel<T> : OrderedItemViewModel
    {
        private protected readonly T _item;
        private readonly Func<T, string> _nameGetter;
        private readonly Func<T, int> _indexGetter;
        private readonly Action<T, int> _indexSetter;
        private readonly Func<int, bool> _indexValidator;
        private int _vmIndex;

        public OrderedItemViewModel(T item,
                                    Func<T, string> nameGetter,
                                    Func<T, int> indexGetter,
                                    Action<T, int> indexSetter,
                                    Func<int, bool>? indexValidator = null)
        {
            _item = item;
            _nameGetter = nameGetter;
            _indexGetter = indexGetter;
            _vmIndex = indexGetter(item);
            _indexSetter = indexSetter;
            _indexValidator = indexValidator ?? (i => true);
        }

        public override string Name => _nameGetter(_item);
        public override int Index
        {
            get
            {
                return _vmIndex;
            }
            set
            {
                _vmIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUpdated));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public override bool IsValid => _indexValidator(Index);

        public override bool IsUpdated => Index != _indexGetter(_item);

        public override event PropertyChangedEventHandler? PropertyChanged;

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
        public override void ResetValues()
        {
            Index = _indexGetter(_item);
        }

        public override void UpdateDatabaseEntities()
        {
            _indexSetter(_item, Index);
        }
    }
}


