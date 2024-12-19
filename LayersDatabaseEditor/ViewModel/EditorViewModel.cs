using LayersIO.Connection;
using LayersIO.Database;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LayersDatabaseEditor.ViewModel
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        private LayersDatabaseContextSqlite? _db;
        private SQLiteLayerDataContextFactory _dbContextFactory;
        private LayerGroupViewModel? _selectedGroup;
        private LayerDataViewModel? _selectedLayer;
        private RelayCommand? _connectCommand;
        private RelayCommand? _disconnectCommand;
        private RelayCommand? _changeSelectedGroupCommand;
        private RelayCommand? _changeSelectedLayerCommand;

        private HashSet<int> _selectedIndexes = new();

        public EditorViewModel(SQLiteLayerDataContextFactory contextFactory)
        {
            _dbContextFactory = contextFactory;
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

        public RelayCommand ChangeSelectedLayerCommand
        {
            get => _changeSelectedLayerCommand ??= new(obj => SelectLayer(obj), obj => IsConnected && SelectedGroup != null);
            set => _changeSelectedLayerCommand = value;
        }


        public ObservableCollection<string> LayerGroupNames { get; private set; } = new();

        public bool IsConnected => _db != null;

        internal LayersDatabaseContextSqlite? Database { get => _db; set => _db = value; }

        public LayerGroupViewModel? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
            }
        }

        public LayerDataViewModel? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                OnPropertyChanged(nameof(SelectedLayer));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void DatabaseConnect(object? obj)
        {
            if (obj == null)
                throw new ArgumentNullException("Отсутствует строка подключения");
            _db = _dbContextFactory.CreateDbContext((string)obj);
            LayerGroupNames.Clear();
            _db.LayerGroups.AsNoTracking()
                           .Select(lg => new { lg.Prefix, lg.MainName })
                           .OrderBy(s => s.Prefix)
                           .ThenBy(s => s.MainName)
                           .AsEnumerable()
                           .Select(s => $"{s.Prefix}_{s.MainName}")
                           .ToList()
                           .ForEach(n => LayerGroupNames.Add(n));
        }

        private void DatabaseDisconnect()
        {
            _db?.Dispose();
            _db = null;
            LayerGroupNames.Clear();
        }

        private void SelectGroup(object? obj)
        {
            if (obj == null)
                return;
            string searchString = (string)obj;
            var match = Regex.Match(searchString, @"(^[^_\s-\.]+)_?(.*$)?");
            string prefix = match.Groups[1].Value;
            string mainName = match.Groups[2].Value;
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
                SelectedGroup = new(result, _db!); // TODO: кэшировать?
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

        private void SelectLayer(object? obj)
        {
            if (obj == null)
            {
                SelectedLayer = null;
                return;
            }
            var layer = (LayerDataViewModel)obj;
            SelectedLayer = layer;
        }
    }
}