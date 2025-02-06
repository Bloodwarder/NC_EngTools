using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
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

namespace LayersDatabaseEditor.ViewModel.Zones
{
    public class SpecialZoneEditorViewModel : IDbRelatedViewModel
    {
        public SpecialZoneEditorViewModel(LayersDatabaseContextSqlite context, LayerGroupViewModel layerGroup)
        {
            Database = context;
            SourceGroup = new(Database.LayerGroups.Find(layerGroup.Id)!);



            HashSet<ZoneInfoData> zoneInfos = Database.LayerGroups.Where(lg => lg.Id == layerGroup.Id)
                                                                  .Include(lg => lg.Layers)
                                                                  .ThenInclude(l => l.Zones)
                                                                  .ThenInclude(z => z.ZoneLayer)
                                                                  .SelectMany(lg => lg.Layers)
                                                                  .SelectMany(l => l.Zones)
                                                                  .Where(z => z.IsSpecial)
                                                                  .ToHashSet();
            var maxStatus = NameParser.LoadedParsers[SourceGroup.Prefix].GetStatusArray().Length;
            var fullGroups = zoneInfos.GroupBy(zi => new { zi.SourceLayer.LayerGroup.Id, zi.ZoneLayerId, zi.AdditionalFilter, zi.Value })
                                      .Where(g => g.Count() == maxStatus)
                                      .AsEnumerable();
            foreach (var group in fullGroups)
            {
                ZoneInfoData first = group.First();
                var sourceVm = new SpecialZoneLayerViewModel(first.SourceLayer.LayerGroup);
                var zoneVm = new SpecialZoneLayerViewModel(first.ZoneLayer);
                var gridItemVm = new SpecialZoneViewModel(sourceVm, zoneVm, first.Value, first.DefaultConstructionWidth, first.AdditionalFilter, Database);
                SpecialZones.Add(gridItemVm);
                foreach (ZoneInfoData item in group)
                    zoneInfos.Remove(item);
            }
            foreach (ZoneInfoData zoneInfo in zoneInfos)
            {
                var sourceVm = new SpecialZoneLayerViewModel(zoneInfo.SourceLayer);
                var zoneVm = new SpecialZoneLayerViewModel(zoneInfo.ZoneLayer);
                var gridItemVm = new SpecialZoneViewModel(sourceVm, zoneVm, zoneInfo.Value, zoneInfo.DefaultConstructionWidth, zoneInfo.AdditionalFilter, Database);
                SpecialZones.Add(gridItemVm);
            }

            SpecialZones.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                        RemovedSpecialZones.AddRange(e.OldItems!.Cast<SpecialZoneViewModel>());
                        break;
                    case NotifyCollectionChangedAction.Add:
                        break;
                }
            };
        }

        public SpecialZoneLayerViewModel SourceGroup { get; set; }
        public LayersDatabaseContextSqlite Database { get; }
        public ObservableCollection<SpecialZoneViewModel> SpecialZones { get; } = new();
        public List<SpecialZoneViewModel> RemovedSpecialZones { get; } = new();
        public List<SpecialZoneViewModel> RegularZones { get; }

        public bool IsValid => throw new NotImplementedException();

        public bool IsUpdated => throw new NotImplementedException();

        public void ResetValues()
        {
            throw new NotImplementedException();
        }

        public void UpdateDatabaseEntities()
        {
            throw new NotImplementedException();
        }
    }

    public class SpecialZoneViewModel : IDbRelatedViewModel
    {
        public SpecialZoneViewModel(SpecialZoneLayerViewModel source,
                                    SpecialZoneLayerViewModel zone,
                                    double value,
                                    double defaultConstructionWidth,
                                    string? filter,
                                    LayersDatabaseContextSqlite context)
        {
            Database = context;
            SourceLayerVm = source;
            ZoneLayerVm = zone;
            Value = value;
            DefaultConstructionWidth = defaultConstructionWidth;
            AdditionalFilter = filter;
        }

        public SpecialZoneLayerViewModel SourceLayerVm { get; set; }
        public SpecialZoneLayerViewModel ZoneLayerVm { get; set; }

        internal LayersDatabaseContextSqlite Database { get; }

        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
        public string? AdditionalFilter { get; set; }

        public bool IsValid => throw new NotImplementedException();

        public bool IsUpdated => throw new NotImplementedException();

        public void ResetValues()
        {
            throw new NotImplementedException();
        }

        public void UpdateDatabaseEntities()
        {
            if (SourceLayerVm.Status == SpecialZoneLayerViewModel.AllStatusesString)
            {
                foreach (var layer in SourceLayerVm.LayerGroup.Layers)
                {
                    var zoneInfo = layer.Zones.FirstOrDefault(z => z.ZoneLayer == ZoneLayerVm.Layer && z.IsSpecial);
                    if (zoneInfo == null)
                    {
                        zoneInfo = new ZoneInfoData()
                        {
                            SourceLayer = layer,
                            ZoneLayer = ZoneLayerVm.Layer,
                        };
                        layer.Zones.Add(zoneInfo);
                    }
                    zoneInfo.DefaultConstructionWidth = DefaultConstructionWidth;
                    zoneInfo.AdditionalFilter = AdditionalFilter;
                    zoneInfo.Value = Value;
                    zoneInfo.IsSpecial = true;
                }
            }
            else
            {
                var layer = SourceLayerVm.Layer;
                var zoneInfo = layer.Zones.FirstOrDefault(z => z.ZoneLayer == ZoneLayerVm.Layer && z.IsSpecial)
                    ?? new ZoneInfoData()
                    {
                        SourceLayer = layer,
                        ZoneLayer = ZoneLayerVm.Layer
                    };
                zoneInfo.DefaultConstructionWidth = DefaultConstructionWidth;
                zoneInfo.AdditionalFilter = AdditionalFilter;
                zoneInfo.Value = Value;
                zoneInfo.IsSpecial = true;
            };
        }
    }


    public class SpecialZoneLayerViewModel : INotifyPropertyChanged
    {
        internal const string AllStatusesString = "Все статусы";

        private string _prefix;
        private string _mainName;
        private string? _status;
        private LayerGroupData _layerGroupData;

        public SpecialZoneLayerViewModel(LayerData layer)
        {
            _prefix = layer.Prefix;
            _mainName = layer.MainName;
            _status = layer.StatusName;
            _layerGroupData = layer.LayerGroup;
        }

        public SpecialZoneLayerViewModel(LayerGroupData layerGroup)
        {
            _prefix = layerGroup.Prefix;
            _mainName = layerGroup.MainName;
            _status = AllStatusesString;
            _layerGroupData = layerGroup;
        }


        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                OnPropertyChanged();
            }
        }
        public string MainName
        {
            get => _mainName;
            set
            {
                _mainName = value;
                OnPropertyChanged();
            }
        }
        public string? Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        internal LayerGroupData LayerGroup => _layerGroupData;
        internal LayerData Layer =>
            Status != AllStatusesString ?
            _layerGroupData.Layers.First(l => l.StatusName == Status) :
            throw new InvalidOperationException("Нельзя найти конкретный слой для элемента с указанным статусом \"Все\"");

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static IEnumerable<string> GetStatusesAddAll(string prefix)
        {
            var statuses = new List<string>() { AllStatusesString };
            statuses.AddRange(NameParser.LoadedParsers[prefix].GetStatusArray());
            return statuses;
        }
        public static IEnumerable<string> GetStatuses(string prefix)
        {
            return NameParser.LoadedParsers[prefix].GetStatusArray();
        }

        public static IEnumerable<string> GetPrefixes()
        {
            return NameParser.LoadedParsers.Keys;
        }
    }
}
