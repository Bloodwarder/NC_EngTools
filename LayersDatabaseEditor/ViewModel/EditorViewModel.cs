using LayersDatabaseEditor.Utilities;
using LayersIO.Connection;
using LayersIO.Database;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace LayersDatabaseEditor.ViewModel
{
    public class EditorViewModel : INotifyPropertyChanged, IDbRelatedViewModel
    {
        private LayersDatabaseContextSqlite? _db;
        private SQLiteLayerDataContextFactory _dbContextFactory;
        private LayerGroupViewModel? _selectedGroup;
        private LayerDataViewModel? _selectedLayer;
        private RelayCommand? _connectCommand;
        private RelayCommand? _disconnectCommand;
        private RelayCommand? _changeSelectedGroupCommand;
        private RelayCommand? _addNewLayerGroupCommand;
        private RelayCommand? _deleteLayerGroupsCommand;
        private RelayCommand? _updateDatabaseCommand;
        private RelayCommand? _resetDatabaseCommand;

        private HashSet<int> _selectedIndexes = new();
        private string _groupInputText = string.Empty;
        private string? _databasePath;

        public EditorViewModel(SQLiteLayerDataContextFactory contextFactory)
        {
            _dbContextFactory = contextFactory;
            UpdatedGroups.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            };
            GroupIdsToDelete.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsUpdated));
            };

        }

        public RelayCommand ConnectCommand
        {
            get => _connectCommand ??= new(obj => DatabaseConnect(obj), obj => true);
            set => _connectCommand = value;
        }
        public RelayCommand DisconnectCommand
        {
            get => _disconnectCommand ??= new(obj => DatabaseDisconnect(), obj => IsConnected);
            set => _disconnectCommand = value;
        }

        public RelayCommand ChangeSelectedGroupCommand
        {
            get => _changeSelectedGroupCommand ??= new(obj => SelectGroup(obj), obj => IsConnected);
            set => _changeSelectedGroupCommand = value;
        }

        public RelayCommand AddNewLayerGroupCommand
        {
            get => _addNewLayerGroupCommand ??= new(obj => AddNewLayerGroup(obj), obj => IsConnected && GroupInputState == GroupInputState.ValidNew);
            set => _addNewLayerGroupCommand = value;
        }

        public RelayCommand DeleteLayerGroupsCommand
        {
            get => _deleteLayerGroupsCommand ??= new(obj => DeleteLayerGroups(obj), obj => IsConnected && (SelectedGroup != null || _selectedIndexes.Any()));
            set => _deleteLayerGroupsCommand = value;
        }

        public RelayCommand UpdateDatabaseCommand
        {
            get => _updateDatabaseCommand ??= new(obj => UpdateDatabaseViewModel(obj), obj => IsConnected 
                                                                                              && ((obj as IDbRelatedViewModel)?.IsValid ?? false)
                                                                                              && ((obj as IDbRelatedViewModel)?.IsUpdated ?? false));
            set => _updateDatabaseCommand = value;
        }

        public RelayCommand ResetDatabaseCommand
        {
            get => _resetDatabaseCommand ??= new(obj => ResetDatabaseViewModel(obj), obj => IsConnected && ((obj as IDbRelatedViewModel)?.IsUpdated ?? false));
            set => _resetDatabaseCommand = value;
        }

        private static void ResetDatabaseViewModel(object? obj)
        {
            if (obj is not IDbRelatedViewModel viewModel)
                return;
            viewModel.ResetValues();
        }

        private void UpdateDatabaseViewModel(object? obj)
        {
            if (obj is not IDbRelatedViewModel viewModel)
                return;
            viewModel.UpdateDbEntity();
            _db!.SaveChanges();
        }

        public ObservableHashSet<string> LayerGroupNames { get; private set; } = new();

        public bool IsConnected => _db != null;
        public bool IsLayerSelected => SelectedLayer != null;
        public bool IsGroupSelected => SelectedGroup != null;

        internal LayersDatabaseContextSqlite? Database { get => _db; set => _db = value; }

        public LayerGroupViewModel? SelectedGroup
        {
            get => _selectedGroup;
            set
            {

                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                OnPropertyChanged(nameof(IsGroupSelected));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public LayerDataViewModel? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                OnPropertyChanged(nameof(SelectedLayer));
                OnPropertyChanged(nameof(IsLayerSelected));
            }
        }

        public ObservableCollection<LayerGroupViewModel> UpdatedGroups { get; private set; } = new();
        public ObservableHashSet<int> GroupIdsToDelete { get; private set; } = new();

        public string GroupInputText
        {
            get => _groupInputText;
            set
            {
                _groupInputText = value;
                OnPropertyChanged(nameof(GroupInputText));
                OnPropertyChanged(nameof(GroupInputState));
            }
        }
        public GroupInputState GroupInputState
        {
            get
            {
                if (LayerGroupNames.Contains(GroupInputText))
                    return GroupInputState.Existing;
                if (string.IsNullOrEmpty(GroupInputText))
                    return GroupInputState.None;
                LayerInfoResult result = NameParser.ParseLayerName(GroupInputText);
                if ((result.Status == LayerInfoParseStatus.Success && result.Value!.Status == null) ||
                    (result.Status == LayerInfoParseStatus.PartialFailure && result.GetExceptions().All(e => e is WrongStatusException)))
                    return GroupInputState.ValidNew;
                else
                    return GroupInputState.Invalid;
            }
        }

        public bool IsValid => (SelectedGroup?.IsValid ?? true) && UpdatedGroups.All(g => g.IsValid);

        public bool IsUpdated => (SelectedGroup?.IsUpdated ?? false) || UpdatedGroups.Any() || GroupIdsToDelete.Any();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void DatabaseConnect(object? obj = null)
        {
            string fileName;
            if (obj == null)
            {
                DirectoryInfo di = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.GetDirectories("UserData").First();
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    DefaultExt = ".db",
                    Filter = "SQLite database files|*.db;*.sqlite",
                    CheckFileExists = true,
                    Multiselect = false,
                    InitialDirectory = di.FullName
                };
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    fileName = ofd.FileName;
                }
                else
                {
                    return;
                }
            }
            else
            {
                fileName = (string)obj;
            }
            _databasePath = fileName;

            _selectedIndexes.Clear();
            UpdatedGroups.Clear();
            GroupIdsToDelete.Clear();
            SelectedLayer = null;
            SelectedGroup = null;

            _db?.Dispose();
            try
            {
                _db = _dbContextFactory.CreateDbContext(fileName);
            }
            catch (Exception ex)
            {
                // Log
                return;
            }

            LayerGroupNames.Clear();
            _db.LayerGroups.AsNoTracking()
                           .Select(lg => new { lg.Prefix, lg.MainName })
                           .OrderBy(s => s.Prefix)
                           .ThenBy(s => s.MainName)
                           .AsEnumerable()
                           .Select(s => $"{s.Prefix}_{s.MainName}")
                           .ToList()
                           .ForEach(n => LayerGroupNames.Add(n));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsUpdated));
        }

        private void DatabaseDisconnect()
        {
            SelectedGroup = null;
            SelectedLayer = null;
            _db?.Dispose();
            _db = null;
            LayerGroupNames.Clear();
            OnPropertyChanged(nameof(IsConnected));
        }

        private void SelectGroup(object? obj)
        {
            if (obj == null)
                return;
            string searchString = (string)obj;
            var match = Regex.Match(searchString, @"(^[^_\s-\.]+)_?(.*$)?");
            string prefix = match.Groups[1].Value;
            string mainName = match.Groups[2].Value;

            if (SelectedGroup?.IsUpdated ?? false)
                UpdatedGroups.Add(SelectedGroup);
            var cachedGroup = UpdatedGroups.Where(g => g.Prefix == prefix && g.MainName == mainName).FirstOrDefault();
            if (cachedGroup != null)
            {
                SelectedGroup = cachedGroup;
                UpdatedGroups.Remove(cachedGroup);
                _selectedIndexes.Clear();
                if (SelectedGroup.Id != 0)
                    _selectedIndexes.Add(SelectedGroup.Id);
            }
            else
            {
                var query = _db?.LayerGroups.Where(lg => lg.Prefix == prefix && lg.MainName.StartsWith(mainName));
                int? count = query?.Count();
                _selectedIndexes.Clear();
                if (count == 1)
                {
                    LayerGroupData result = query!.Include(lg => lg.Layers)
                                                 .ThenInclude(l => l.Zones)
                                                 .ThenInclude(z => z.ZoneLayer)
                                                 .First();
                    _selectedIndexes.Add(result.Id);

                    LayerGroupViewModel layerGroupViewModel = new(result, _db!);
                    layerGroupViewModel.PropertyChanged += (s, e) =>
                    {
                        OnPropertyChanged(nameof(IsUpdated));
                        OnPropertyChanged(nameof(IsValid));
                    };
                    SelectedGroup = layerGroupViewModel; // TODO: кэшировать?
                }
                else if (count > 1)
                {
                    _selectedIndexes = query!.Select(lg => lg.Id).ToHashSet();
                    SelectedGroup = null;
                    SelectedLayer = null;
                }
                else
                {
                    SelectedGroup = null;
                    SelectedLayer = null;
                }
            }
        }
        private void DeleteLayerGroups(object? obj)
        {
            int count = _selectedIndexes.Count;
            if (count > 1)
            {
                MessageBoxResult result = MessageBox.Show($"Узел содержит {count} групп слоёв. Действительно удалить узел целиком?",
                                                          "Внимание",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            foreach (var id in _selectedIndexes)
                GroupIdsToDelete.Add(id);
            var entitiesToRemove = Database!.LayerGroups.Where(g => _selectedIndexes.Contains(g.Id)).AsEnumerable();
            var namesToRemove = entitiesToRemove.Select(g => g.Name);
            var cachedToRemove = UpdatedGroups.Where(g => _selectedIndexes.Contains(g.Id)).AsEnumerable();

            foreach (var entityToRemove in cachedToRemove)
                UpdatedGroups.Remove(entityToRemove);

            foreach (string name in namesToRemove)
                LayerGroupNames.Remove(name);

            SelectedGroup = null;
            SelectedLayer = null;
        }

        private void AddNewLayerGroup(object? obj)
        {
            string? groupName = obj as string;
            if (groupName == null)
                return;
            if (SelectedGroup != null && SelectedGroup.IsUpdated)
                UpdatedGroups.Add(SelectedGroup);
            LayerGroupNames.Add(groupName);
            SelectedGroup = new LayerGroupViewModel(groupName, Database!);
        }

        public void UpdateDbEntity()
        {
            if (Database == null)
                return;

            var entitiesToDelete = Database.LayerGroups.Where(g => GroupIdsToDelete.ExposedSet.Contains(g.Id)).AsEnumerable();
            Database.LayerGroups.RemoveRange(entitiesToDelete);
            GroupIdsToDelete.Clear();

            foreach (var model in UpdatedGroups)
                model.UpdateDbEntity();

            SelectedGroup?.UpdateDbEntity();
        }

        public void ResetValues()
        {
            ConnectCommand.Execute(_databasePath);
        }

        public void UpdateUI()
        {
            OnPropertyChanged("");
        }
    }
}