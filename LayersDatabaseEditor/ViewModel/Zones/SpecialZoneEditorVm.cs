using LayersDatabaseEditor.UI;
using LayersIO.Connection;
using LayersIO.Model;
using LoaderCore.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NameClassifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LayersDatabaseEditor.ViewModel.Zones
{
    public class SpecialZoneEditorVm : IDbRelatedViewModel, INotifyPropertyChanged
    {
        private RelayCommand? _addSpecialZoneCommand;
        private RelayCommand? _removeSpecialZoneCommand;
        private RelayCommand? _updateDatabaseCommand;
        public SpecialZoneEditorVm(LayersDatabaseContextSqlite context, LayerGroupVm layerGroup)
        {
            Database = context;
            SourceGroup = new(Database.LayerGroups.Find(layerGroup.Id)!);

            LayerNames = Database.LayerGroups.OrderBy(g => g.Prefix)
                                             .ThenBy(g => g.MainName)
                                             .Select(g => new SimpleLayer(g.Prefix, g.MainName, null))
                                             .ToArray();
            var collection = Database.LayerGroups.Where(g => g.Id == layerGroup.Id)
                                                  .SelectMany(g => g.Layers)
                                                  .SelectMany(l => l.Zones)
                                                  .Where(z => !z.IsSpecial)
                                                  .Include(z => z.ZoneLayer)
                                                  .ThenInclude(l => l.LayerGroup)
                                                  .OrderBy(z => z.SourceLayer.StatusName)
                                                  .ThenBy(z => z.ZoneLayer.LayerGroup.Prefix)
                                                  .ThenBy(z => z.ZoneLayer.LayerGroup.MainName)
                                                  .Select(z => new { SourceStatus = z.SourceLayer.StatusName, ZoneName = z.ZoneLayer.Name })
                                                  .AsEnumerable();
            RegularZones = new(collection);

            HashSet<ZoneInfoData> zoneInfos = Database.LayerGroups.Where(lg => lg.Id == layerGroup.Id)
                                                                  .Include(lg => lg.Layers)
                                                                  .ThenInclude(l => l.Zones)
                                                                  .ThenInclude(z => z.ZoneLayer)
                                                                  .ThenInclude(z => z.LayerGroup)
                                                                  .SelectMany(lg => lg.Layers)
                                                                  .SelectMany(l => l.Zones)
                                                                  .Where(z => z.IsSpecial)
                                                                  .ToHashSet();

            var maxStatus = NameParser.LoadedParsers[SourceGroup.Prefix!].GetStatusArray().Length;
            var fullGroups = zoneInfos.GroupBy(zi => new { zi.SourceLayer.LayerGroup.Id, zi.ZoneLayerId, zi.AdditionalFilter, zi.Value })
                                      .Where(g => g.Count() == maxStatus)
                                      .AsEnumerable();
            PropertyChangedEventHandler handler = (s, e) =>
            {
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            };
            foreach (var group in fullGroups)
            {
                var gridItemVm = new SpecialZoneVm(group, Database);
                gridItemVm.PropertyChanged += handler;
                SpecialZones.Add(gridItemVm);
                foreach (ZoneInfoData item in group)
                    zoneInfos.Remove(item);
            }
            foreach (ZoneInfoData zoneInfo in zoneInfos)
            {
                var gridItemVm = new SpecialZoneVm(zoneInfo, Database);
                gridItemVm.PropertyChanged += handler;
                SpecialZones.Add(gridItemVm);
            }

            SpecialZones.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                        RemovedSpecialZones.AddRange(e.OldItems!.Cast<SpecialZoneVm>());
                        break;
                    case NotifyCollectionChangedAction.Add:
                        break;
                }
            };
        }

        public RelayCommand AddSpecialZoneCommand
        {
            get => _addSpecialZoneCommand ??= new(obj => AddSpecialZone(obj), obj => true);
            set => _addSpecialZoneCommand = value;
        }

        public RelayCommand RemoveSpecialZoneCommand
        {
            get => _removeSpecialZoneCommand ??= new(obj => RemoveSpecialZone(obj), obj => obj != null);
            set => _removeSpecialZoneCommand = value;
        }

        public RelayCommand UpdateDatabaseCommand
        {
            get => _updateDatabaseCommand ??= new(obj => UpdateDatabaseEntities(), obj => IsUpdated); // записи, не прошедшие валидацию, удалятся из таблицы
            set => _updateDatabaseCommand = value;
        }


        public SimpleLayer[] LayerNames { get; set; }
        public SpecialZoneLayerVm SourceGroup { get; set; }
        public LayersDatabaseContextSqlite Database { get; }
        public ObservableCollection<SpecialZoneVm> SpecialZones { get; } = new();
        public List<SpecialZoneVm> RemovedSpecialZones { get; } = new();
        public ObservableCollection<object> RegularZones { get; private set; }

        public bool IsValid => SpecialZones.All(z => z.IsValid);

        public bool IsUpdated => SpecialZones.Any(z => z.IsUpdated) || RemovedSpecialZones.Any();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ResetValues()
        {
            var zones = SpecialZones.ToArray();
            foreach (var z in zones)
            {
                if (!z.ZoneInfos.Any())
                {
                    SpecialZones.Remove(z);
                }
                else
                {
                    z.ResetValues();
                }
            }
            RemovedSpecialZones.Clear();
        }

        public void UpdateDatabaseEntities()
        {
            using (var transaction = Database.Database.BeginTransaction())
            {
                try
                {
                    var zoneInfos = RemovedSpecialZones.SelectMany(z => z.ZoneInfos).AsEnumerable();
                    Database.Zones.RemoveRange(zoneInfos);
                    RemovedSpecialZones.Clear();
                    foreach (var specialZone in SpecialZones)
                    {
                        if (!specialZone.IsValid)
                            SpecialZones.Remove(specialZone);
                    }
                    foreach (var specialZone in SpecialZones)
                    {
                        specialZone.UpdateDatabaseEntities();
                    }
                    Database.SaveChanges();
                    transaction.Commit();
                    foreach (var specialZone in SpecialZones)
                    {
                        specialZone.SourceLayerVm.InitialStatus = specialZone.SourceLayerVm.Status;
                        specialZone.ZoneLayerVm.InitialStatus = specialZone.ZoneLayerVm.Status;
                    }
                    OnPropertyChanged(nameof(IsUpdated));
                }
                catch (DbUpdateException ex)
                {
                    MessageBox.Show($"Ошибка обновления: {ex.Message}");
                    transaction.Rollback();
                }
            }
        }

        internal void AddSpecialZone(object? obj)
        {
            SpecialZoneVm zone = new((SpecialZoneLayerVm)SourceGroup.Clone(), Database);
            SpecialZones.Add(zone);
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(IsUpdated));
            zone.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            };
        }

        internal void RemoveSpecialZone(object? obj)
        {
            if (obj is not SpecialZoneVm zone)
                return;

            SpecialZones.Remove(zone);
            if (zone.ZoneInfos.Any())
            {
                RemovedSpecialZones.Add(zone);
            }
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(IsUpdated));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
