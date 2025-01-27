using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections;
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
        private readonly LayerGroupData _layerGroup;

        private RelayCommand? _saveAndExitCommand;
        private RelayCommand? _toggleZoneInfoEnabledCommand;
        private RelayCommand? _toggleIgnoreConstructionWidthCommand;
        private Visibility _isSourceVisible;
        private Visibility _isZoneVisible;

        public ZoneEditorViewModel(LayerGroupData layerGroup, LayersDatabaseContextSqlite context, ZoneRelation zoneRelation)
        {
            _layerGroup = layerGroup;
            _db = context;
            var groups = Database.LayerGroups.Include(g => g.Layers)
                                             .ThenInclude(l => l.Zones)
                                             .ThenInclude(z => z.ZoneLayer)
                                             .Where(g => g != layerGroup)
                                             .OrderBy(g => g.Prefix)
                                             .ThenBy(g => g.MainName)
                                             .AsEnumerable();
            var mappings = Database.ZoneMappings.Where(m => m.TargetPrefix == layerGroup.Prefix).AsEnumerable();
            switch (zoneRelation)
            {
                case ZoneRelation.ZoneGroup:
                    foreach (var group in groups)
                    {
                        Zones.Add(new(_layerGroup, group, _db, mappings));
                    }
                    IsSourceVisible = Visibility.Visible;
                    IsZoneVisible = Visibility.Collapsed;
                    EditorCaption = $"Источники для слоя зон {_layerGroup.Name}";
                    break;
                case ZoneRelation.SourceGroup:
                    foreach (var group in groups)
                    {
                        Zones.Add(new(group, _layerGroup, _db, mappings));
                    }
                    IsSourceVisible = Visibility.Collapsed;
                    IsZoneVisible = Visibility.Visible;
                    EditorCaption = $"Зоны для слоя {_layerGroup.Name}";
                    break;
            }
        }

        public string EditorCaption { get; private set; }

        public Visibility IsSourceVisible
        {
            get => _isSourceVisible;
            private set
            {
                _isSourceVisible = value;
                OnPropertyChanged();
            }
        }
        public Visibility IsZoneVisible
        {
            get => _isZoneVisible;
            private set
            {
                _isZoneVisible = value;
                OnPropertyChanged();
            }
        }


        public RelayCommand SaveAndExitCommand
        {
            get => _saveAndExitCommand ??= new(obj => SaveAndExit(obj), obj => true);
            set => _saveAndExitCommand = value;
        }

        public RelayCommand ToggleZoneInfoEnabledCommand
        {
            get => _toggleZoneInfoEnabledCommand ??= new(obj => ZoneInfoAction(obj, zi => zi.IsActivated = !zi.IsActivated),
                                                         obj => obj != null);
            set => _toggleZoneInfoEnabledCommand = value;
        }

        public RelayCommand ToggleIgnoreConstructionWidthCommand
        {
            get => _toggleIgnoreConstructionWidthCommand ??= new(obj => ZoneInfoAction(obj, zi => zi.IgnoreConstructionWidth = !zi.IgnoreConstructionWidth),
                                                                 obj => obj != null);
            set => _toggleIgnoreConstructionWidthCommand = value;
        }

        private void ZoneInfoAction(object? collection, Action<ZoneGroupInfoViewModel> action)
        {
            if (collection == null)
                return;
            var infos = ((IEnumerable)collection).Cast<ZoneGroupInfoViewModel>();
            foreach (var info in infos)
            {
                action(info);
            }
            OnPropertyChanged(nameof(Zones));
        }

        /// <summary>
        /// Сохранить изменения в базе и закрыть окно
        /// </summary>
        /// <param name="obj">Объект Window, подлежащий закрытия</param>
        private void SaveAndExit(object? obj)
        {
            if (obj is not Window window)
                return;

            foreach (var zone in Zones)
            {
                zone.UpdateDatabaseEntities();
            }
            try
            {
                Database.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log ex.Message
            }

            window.Close();
        }

        internal LayersDatabaseContextSqlite Database => _db;

        public ObservableCollection<ZoneGroupInfoViewModel> Zones { get; set; } = new();


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
