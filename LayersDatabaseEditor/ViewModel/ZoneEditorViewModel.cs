using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LayersDatabaseEditor.ViewModel
{
    public class ZoneEditorViewModel : INotifyPropertyChanged
    {
        private readonly LayersDatabaseContextSqlite _db;
        private readonly LayerGroupData _zoneSourceGroup;

        private RelayCommand? _saveAndExitCommand;

        public ZoneEditorViewModel(LayerGroupData sourceLayerGroup, LayersDatabaseContextSqlite context)
        {
            _zoneSourceGroup = sourceLayerGroup;
            _db = context;
            var groups = Database.LayerGroups.Include(g => g.Layers)
                                             .ThenInclude(l => l.Zones)
                                             .ThenInclude(z => z.ZoneLayer)
                                             .Where(g => g != sourceLayerGroup)
                                             .OrderBy(g => g.Prefix)
                                             .ThenBy(g => g.MainName)
                                             .AsEnumerable();
            foreach (var group in groups)
            {
                Zones.Add(new(_zoneSourceGroup, group, _db));
            }
        }

        public RelayCommand SaveAndExitCommand
        {
            get => _saveAndExitCommand ??= new(obj => SaveAndExit(obj), obj => true);
            set => _saveAndExitCommand = value;
        }

        private void SaveAndExit(object? obj)
        {
            if (obj is not Window window)
                return;

            foreach (var zone in Zones)
            {
                zone.UpdateDatabaseEntities();
            }
            Database.SaveChanges();

            window.Close();
        }

        internal LayersDatabaseContextSqlite Database => _db;

        public ObservableCollection<ZoneInfoViewModel> Zones { get; set; } = new();


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
