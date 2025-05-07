using LayersIO.Connection;
using LayersIO.Model;
using LoaderCore.UI;
using Microsoft.EntityFrameworkCore;
using Npoi.Mapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class GroupOrderingWindowVm : IDbRelatedViewModel, INotifyPropertyChanged
    {
        private DisplayedLayers _displayedLayersState;
        private string? _layerFilterString;
        private DrawOrderGroupVm? _selectedGroup;
        private readonly Func<LayerGroupedVm, bool> _textPredicate;
        private readonly Func<LayerGroupedVm, bool> _groupedPredicate;
        private readonly Func<LayerGroupedVm, bool> _ungroupedPredicate;

        public GroupOrderingWindowVm(LayersDatabaseContextSqlite context)
        {
            Database = context;

            ResetValues();

            GroupsView = new(Groups);
            GroupsView.SortDescriptions.Add(new(nameof(DrawOrderGroupVm.Index), ListSortDirection.Ascending));
            GroupsView.IsLiveSorting = true;

            LayersView = new(Layers);

            _textPredicate = l => string.IsNullOrEmpty(LayerFilterString) || l.Name.Contains(LayerFilterString, StringComparison.CurrentCultureIgnoreCase);
            _groupedPredicate = l => l.Group == SelectedGroup;//l => SelectedGroup?.Layers.Contains(l.Name) ?? false;
            _ungroupedPredicate = l => l.Group == null;

            LayersView.SortDescriptions.Add(new(nameof(LayerOrderedVm.Name), ListSortDirection.Ascending));
            LayersView.Filter = l => _textPredicate((LayerGroupedVm)l) && _ungroupedPredicate((LayerGroupedVm)l);
            LayersView.IsLiveFiltering = true;

            MoveUpCommand = new(obj => MoveUp(), obj => CanMoveUp());
            MoveDownCommand = new(obj => MoveDown(), obj => CanMoveDown());
            AddGroupCommand = new(obj => AddGroup(), obj => CanAddGroup());
            RemoveGroupCommand = new(obj => RemoveGroup(), obj => CanRemoveGroup());
            SaveChangesCommand = new(obj => UpdateDatabaseEntities(), obj => IsValid && IsUpdated);
        }

        internal DisplayedLayers DisplayedLayersState
        {
            get => _displayedLayersState;
            set
            {
                _displayedLayersState = value;
                switch (value)
                {
                    case DisplayedLayers.All:
                        LayersView.Filter = l => _textPredicate((LayerGroupedVm)l);
                        break;
                    case DisplayedLayers.Ungrouped:
                        LayersView.Filter = l => _textPredicate((LayerGroupedVm)l) && _ungroupedPredicate((LayerGroupedVm)l);

                        break;
                    case DisplayedLayers.Grouped:
                        LayersView.Filter = l => _textPredicate((LayerGroupedVm)l) && _groupedPredicate((LayerGroupedVm)l);
                        break;
                }
                LayersView.Dispatcher.Invoke(() => LayersView.Refresh(), DispatcherPriority.Background);
            }
        }


        public string? LayerFilterString
        {
            get => _layerFilterString;
            set
            {
                _layerFilterString = value;
                OnPropertyChanged();
            }
        }

        public DrawOrderGroupVm? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                using (LayersView.DeferRefresh())
                {
                    foreach (var layer in Layers)
                        layer.IncludeState = layer.Group != null && layer.Group == SelectedGroup;//SelectedGroup?.Layers.Contains(layer.Name) ?? false;
                }
                LayersView.Dispatcher.Invoke(() => LayersView.Refresh(), DispatcherPriority.Background);

                OnPropertyChanged();
            }
        }

        public ListCollectionView LayersView { get; }
        public ListCollectionView GroupsView { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<LayerGroupedVm> Layers { get; } = new();

        public ObservableCollection<DrawOrderGroupVm> Groups { get; } = new();
        public List<DrawOrderGroupVm> GroupsToRemove { get; } = new();

        public bool IsValid => Groups.All(g => g.IsValid) && Layers.All(l => l.IsValid);

        public bool IsUpdated => Groups.Any(g => g.IsUpdated) || Layers.Any(l => l.IsUpdated);

        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand AddGroupCommand { get; }
        public RelayCommand RemoveGroupCommand { get; }
        public RelayCommand SaveChangesCommand { get; }

        internal LayersDatabaseContextSqlite Database { get; }

        public void UpdateDatabaseEntities()
        {
            using (var transaction = Database.Database.BeginTransaction())
            {
                try
                {
                    foreach (var group in GroupsToRemove)
                    {
                        Database.Remove(group.DrawOrderGroup);
                    }
                    foreach (var group in Groups)
                    {
                        if (Database.Entry(group.DrawOrderGroup).State == EntityState.Detached)
                            Database.DrawOrderGroups.Add(group.DrawOrderGroup);
                        group.UpdateDatabaseEntities();
                    }
                    foreach (var layer in Layers)
                    {
                        layer.UpdateDatabaseEntities();
                    }
                    Database.SaveChanges();
                    transaction.Commit();
                    GroupsToRemove.Clear();
                }
                catch (Exception ex)
                {
                    // log
                    transaction.Rollback();
                    MessageBox.Show(ex.Message, "Ошибка обновления БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void ResetValues()
        {
            DrawOrderGroupVm.NameMap.Clear();

            var groups = Database.DrawOrderGroups.AsEnumerable().Select(g => CreateGroupVm(g));
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            var layers = Database.Layers.Include(l => l.LayerGroup).AsEnumerable().Select(l => new LayerGroupedVm(l));
            Layers.Clear();
            foreach (var layer in layers)
            {
                Layers.Add(layer);
            }

            GroupsToRemove.Clear();
        }

        internal DrawOrderGroupVm CreateGroupVm(DrawOrderGroup? group = null)
        {
            DrawOrderGroupVm newGroup;
            if (group == null)
            {
                newGroup = new DrawOrderGroupVm();
            }
            else
            {
                newGroup = new DrawOrderGroupVm(group);
            }
            newGroup.PropertyChanged += OnIndexChanged;
            return newGroup;
        }



        internal void ChangeLayerIncludeState(bool state, IEnumerable<LayerGroupedVm> layers)
        {
            if (SelectedGroup == null)
                return;

            foreach (var layer in layers)
            {
                layer.IncludeState = state;
                if (state)
                {
                    layer.Group = SelectedGroup;
                }
                else
                {
                    layer.Group = null;
                }
            }
            LayersView.Dispatcher.Invoke(() => LayersView.Refresh(), DispatcherPriority.Background);
        }

        internal void ImportIndexFromDrawProperties()
        {
            DrawOrderGroupVm.NameMap.Clear();
            Groups.Clear();
            Layers.Clear();
            GroupsToRemove.Clear();
            var layers = Database.Layers.Include(l => l.DrawOrderGroup)
                                        .Include(l => l.LayerGroup)
                                        .AsEnumerable()
                                        .GroupBy(l => l.LayerPropertiesData.DrawOrderIndex)
                                        .OrderBy(g => g.Key);
            int counter = 0;
            foreach (var groupedLayer in layers)
            {
                DrawOrderGroupVm? group;
                if (groupedLayer.Key != 0)
                {
                    counter++;
                    group = CreateGroupVm();
                    group.Name = $"Новая группа {counter}";
                    group.SetIndexDeferRecalc(counter);

                    Groups.Add(group);
                }
                else
                {
                    group = null;
                }

                foreach (var layer in groupedLayer)
                {
                    LayerGroupedVm layerVm = new(layer)
                    {
                        Group = group
                    };
                    Layers.Add(layerVm);
                }
            }
            Dispatcher.CurrentDispatcher.Invoke(() => GroupsView.Refresh(), DispatcherPriority.Background);
        }

        private bool CanMoveDown()
        {
            if (SelectedGroup == null)
                return false;
            if (SelectedGroup.Index == Groups.Max(g => g.Index))
                return false;
            return true;
        }
        private void MoveDown()
        {
            if (!CanMoveDown())
                return;
            SelectedGroup!.Index++;
        }

        private bool CanMoveUp()
        {
            if (SelectedGroup == null)
                return false;
            if (SelectedGroup.Index < 2)
                return false;
            return true;
        }
        private void MoveUp()
        {
            if (!CanMoveUp())
                return;
            SelectedGroup!.Index--;
        }

        private bool CanAddGroup()
        {
            if (Groups.Any(g => g.Name == "Новая группа"))
                return false;
            return true;
        }

        private void AddGroup()
        {
            DrawOrderGroupVm group = CreateGroupVm();
            Groups.Add(group);
        }

        private bool CanRemoveGroup()
        {
            return SelectedGroup != null;
        }
        private void RemoveGroup()
        {
            var group = SelectedGroup;
            Groups.Remove(group!);
            if (DrawOrderGroupVm.NameMap.ContainsKey(group!.Name))
                DrawOrderGroupVm.NameMap.Remove(group!.Name);
            if (group!.DrawOrderGroup.Id != 0)
                GroupsToRemove.Add(group);

            var groupsToRecalcIndex = Groups.Where(g => g.Index > group!.Index);
            foreach (var g in groupsToRecalcIndex)
                g.SetIndexDeferRecalc(g.Index-1);

            var layersToSetNullGroup = Layers.Where(l => l.Group == group);
            foreach (var layer in layersToSetNullGroup)
                layer.Group = null;
        }

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        private void OnIndexChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e is not DrawOrderGroupVm.IndexChangedEventArgs args)
                return;

            int increment = args.NewIndex - args.OldIndex;
                if (args.NewIndex == 0) // старый не может быть 0 - событие не вызовется
                {
                    var groupsToReduce = Groups.Where(g => g.Index > args.OldIndex).ToList();
                    groupsToReduce.ForEach(g => g.SetIndexDeferRecalc(g.Index - 1));
                }
                else if (args.OldIndex == 0)
                {
                    var groupsToIncrease = Groups.Where(g => g != sender && g.Index >= args.NewIndex).ToList();
                    groupsToIncrease.ForEach(g => g.SetIndexDeferRecalc(g.Index + 1));
                }
                else if (increment > 0)
                {
                    var groupsToReduce = Groups.Where(g => g != sender && g.Index > args.OldIndex && g.Index <= args.NewIndex);
                    groupsToReduce.ForEach(g => g.SetIndexDeferRecalc(g.Index - 1));

                }
                else if (increment < 0)
                {
                    var groupsToIncrease = Groups.Where(g => g != sender && g.Index >= args.NewIndex && g.Index < args.OldIndex).ToList();
                    groupsToIncrease.ForEach(g => g.SetIndexDeferRecalc(g.Index + 1));
                }
        }

        private void IndexFix()
        {
            var groups = Groups.Where(g => g.Index != 0).OrderBy(g => g.Index);
            int i = 1;
            using (var defer = GroupsView.DeferRefresh())
            {
                foreach (var group in groups)
                {
                    group.SetIndexDeferRecalc(i++);
                }
            }
            Dispatcher.CurrentDispatcher.Invoke(() => GroupsView.Refresh(), DispatcherPriority.Background);
        }

    }
}
