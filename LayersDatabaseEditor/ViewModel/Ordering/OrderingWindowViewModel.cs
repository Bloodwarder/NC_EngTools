using LayersIO.Connection;
using LayersIO.Model;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class OrderingWindowViewModel : IDbRelatedViewModel, INotifyPropertyChanged
    {

        private readonly ObservableCollection<OrderedItemViewModel> _items = new();
        private readonly ListCollectionView _itemsView;
        private OrderedItemViewModel? _selectedItem;
        private readonly int _itemsCount;
        private readonly bool _splitEqualIndexes;


        private OrderingWindowViewModel(LayersDatabaseContextSqlite context, bool splitEqualIndexes = true)
        {
            _itemsView = new ListCollectionView(_items);
            _itemsView.SortDescriptions.Add(new(nameof(OrderedItemViewModel.Index), ListSortDirection.Ascending));
            _itemsView.SortDescriptions.Add(new(nameof(OrderedItemViewModel.Name), ListSortDirection.Ascending));
            _itemsView.IsLiveSorting = true;
            _itemsView.LiveSortingProperties.Add(nameof(OrderedItemViewModel.Index));

            MoveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand(MoveDown, CanMoveDown);
            RebuildIndexesCommand = new RelayCommand(obj => RebuildIndexes(), obj => true);
            SaveChangesCommand = new RelayCommand(obj => UpdateDatabaseEntities(), obj => IsValid && IsUpdated);
            ResetChangesCommand = new RelayCommand(obj => ResetValues(), obj => IsUpdated);
            Database = context;
            _splitEqualIndexes = splitEqualIndexes;
        }

        public OrderingWindowViewModel(IEnumerable<LayerData> layers, LayersDatabaseContextSqlite context, bool splitEqualIndexes = true) : this(context, splitEqualIndexes)
        {
            foreach (var layer in layers.OrderBy(lg => lg.LayerPropertiesData.DrawOrderIndex).AsEnumerable())
            {
                _items.Add(new LayerDataDrawOrderViewModel(layer));
                _itemsCount++;
            }
        }

        public OrderingWindowViewModel(IEnumerable<LayerGroupData> layerGroups, LayersDatabaseContextSqlite context, bool splitEqualIndexes = true) : this(context, splitEqualIndexes)
        {
            foreach (var layerGroup in layerGroups.OrderBy(lg => lg.LayerLegendData.Rank))
            {
                _items.Add(new LayerGroupLegendTableOrderViewModel(layerGroup));
                _itemsCount++;
            }
        }

        internal LayersDatabaseContextSqlite Database { get; }

        public bool IsValid => _items.All(item => item.IsValid);

        public bool IsUpdated => _items.Any(item => item.IsUpdated);

        public ListCollectionView ItemsView => _itemsView;

        public OrderedItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            }
        }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RebuildIndexesCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand ResetChangesCommand { get; }

        private bool CanMoveUp(object? _) => SelectedItem != null && _itemsView.GetItemAt(0) != SelectedItem;
        private bool CanMoveDown(object? _) => SelectedItem != null && _itemsView.GetItemAt(_itemsCount - 1) != SelectedItem;

        public event PropertyChangedEventHandler? PropertyChanged;


        private void MoveUp(object? _)
        {
            var target = (OrderedItemViewModel)ItemsView.GetItemAt(ItemsView.IndexOf(SelectedItem) - 1);
            if (target != null)
            {
                if (target.Index == SelectedItem!.Index)
                {
                    SelectedItem.Index--;
                }
                else
                {
                    SwapIndexes(SelectedItem!, target);
                }
            }
        }

        private void MoveDown(object? _)
        {
            var target = (OrderedItemViewModel)ItemsView.GetItemAt(ItemsView.IndexOf(SelectedItem) + 1);
            if (target != null)
            {
                if (target.Index == SelectedItem!.Index)
                {
                    SelectedItem.Index++;
                }
                else
                {
                    SwapIndexes(SelectedItem!, target);
                }
            }
        }

        private void SwapIndexes(OrderedItemViewModel a, OrderedItemViewModel b)
        {
            // Efficient swap without triggering multiple sorts
            (a.Index, b.Index) = (b.Index, a.Index);

            // Refresh view once after both changes
            _itemsView.Refresh();
        }

        private void RebuildIndexes()
        {
            int index = 100;
            var collection = _items.OrderBy(item => item.Index).ThenBy(item => item.Name).ToArray();

            var firstItem = collection[0];
            var separators = NameParser.LoadedParsers.ToDictionary(p => p.Key, p => p.Value.Separator);
            _ = separators.TryGetValue(NameParser.GetPrefix(firstItem.Name) ?? "", out string? separator);
            string[] prevDecomp = firstItem.Name.Split(separator ?? "_");

            using (var deferRefresh = ItemsView.DeferRefresh())
            {
                //previousItem.Index = index;

                var item = collection[1];
                for (int i = 1; i < _itemsCount; i++)
                {
                    _ = separators.TryGetValue(NameParser.GetPrefix(item.Name) ?? "", out separator);
                    string[] decomp = item.Name.Split(separator ?? "_");

                    //if (previousItem.Index == item.Index && !_splitEqualIndexes)
                    //    continue;

                    if (decomp[0] != prevDecomp[0])
                        index = index + 1000 - index % 1000;
                    else if (decomp == prevDecomp)
                        index++;
                    else if (decomp[1] != prevDecomp[1])
                        index = index + 500 - index % 500;
                    else if (decomp[^1] != prevDecomp[^1])
                        index = index + 10 - index % 10;
                    else
                        index += 100;
                    if (_splitEqualIndexes)
                    {
                        item.Index = index;
                        prevDecomp = decomp;
                        firstItem = item;
                        item = collection[i + 1];
                    }
                    else
                    {
                        int initialIndex = item.Index;
                        bool shifted = false;
                        while (i < _itemsCount - 1 && item.Index == initialIndex)
                        {
                            item.Index = index;
                            prevDecomp = decomp;
                            firstItem = item;
                            item = collection[++i];
                            shifted = true;
                        }
                        i = shifted ? i - 1 : i;
                    }
                }
            }
        }

        public void ResetValues()
        {
            foreach (var item in _items)
                item.ResetValues();
        }

        public void UpdateDatabaseEntities()
        {
            using (var transaction = Database.Database.BeginTransaction())
            {
                try
                {
                    foreach (var item in _items)
                        item.UpdateDatabaseEntities();

                    Database.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    transaction.Rollback();
                }
            }
        }
    }
}
