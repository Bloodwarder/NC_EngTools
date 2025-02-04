using LayersDatabaseEditor.ViewModel.Validation;
using LayersIO.Connection;
using LayersIO.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel.Zones
{
    public class ZoneGroupInfoViewModel
    {
        private readonly LayersDatabaseContextSqlite _db;
        private bool _isActivated;
        private readonly LayerGroupData _zoneGroup;
        private readonly LayerGroupData _sourceGroup;
        private readonly List<ZoneInfoData> _zoneInfoData = new();
        private double _defaultConstructionWidth;
        private double _value;
        private bool _ignoreConstructionWidth = true;
        private string _zoneLayerName = null!;
        private string _sourceLayerName = null!;

        private readonly ZoneMapping[]? _mappings;

        public ZoneGroupInfoViewModel(LayerGroupData zoneGroup, LayerGroupData sourceGroup, LayersDatabaseContextSqlite context, IEnumerable<ZoneMapping> mappings)
        {
            _db = context;
            _zoneGroup = zoneGroup;
            _sourceGroup = sourceGroup;
            _mappings = mappings.Where(m => m.SourcePrefix == sourceGroup.Prefix).ToArray();
            SourceLayerName = sourceGroup.Name;
            ZoneLayerName = zoneGroup.Name;
            var valuesInfo = sourceGroup.Layers.FirstOrDefault(l => l.Zones.Any(z => z.ZoneLayer.LayerGroup == zoneGroup))?.Zones
                                               .Where(z => z.ZoneLayer.LayerGroup == zoneGroup && !z.IsSpecial)
                                               .AsEnumerable();
            if (valuesInfo != null)
            {
                IsActivated = true;
                _zoneInfoData.AddRange(valuesInfo);
                Value = valuesInfo.First().Value;
                DefaultConstructionWidth = valuesInfo.First().DefaultConstructionWidth;
                IgnoreConstructionWidth = valuesInfo.First().IgnoreConstructionWidth;
            }
        }

        public static ZoneInfoViewModelValidator Validator { get; } = new();
        internal LayersDatabaseContextSqlite Database => _db;

        public bool IsActivated
        {
            get => _isActivated;
            set
            {
                _isActivated = value;
                OnPropertyChanged();
            }
        }

        public string SourceLayerName
        {
            get => _sourceLayerName;
            set
            {
                _sourceLayerName = value;
                OnPropertyChanged();
            }
        }

        public string ZoneLayerName
        {
            get => _zoneLayerName;
            set
            {
                _zoneLayerName = value;
                OnPropertyChanged();
            }
        }
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public double DefaultConstructionWidth
        {
            get => _defaultConstructionWidth;
            set
            {
                _defaultConstructionWidth = value;
                OnPropertyChanged();
            }
        }

        public bool IgnoreConstructionWidth
        {
            get => _ignoreConstructionWidth;
            set
            {
                _ignoreConstructionWidth = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void UpdateDatabaseEntities()
        {
            var validationResult = Validator.Validate(this);
            if (!validationResult.IsValid)
            {
                // TODO: LOG validationResult.Errors
                return;
            }

            foreach (var layer in _sourceGroup.Layers)
            {
                var zones = layer.Zones.Where(z => z.ZoneLayer.LayerGroup == _zoneGroup);
                if (!IsActivated)
                {
                    // если деактивировано - удалить все записи
                    if (zones.Any())
                    {
                        zones.ToList().ForEach(z => layer.Zones.Remove(z));
                        Database.Zones.RemoveRange(zones);
                    }
                    continue;
                }
                var mappings = _mappings!.Where(m => m.SourceStatus == layer.StatusName);
                foreach (var mapping in mappings)
                {
                    var zone = zones.Where(z => z.SourceLayer.StatusName == mapping.SourceStatus && z.ZoneLayer.StatusName == mapping.TargetStatus).FirstOrDefault();
                    if (zone == null)
                    {
                        zone = new ZoneInfoData()
                        {
                            Value = Value,
                            AdditionalFilter = mapping.AdditionalFilter,
                            DefaultConstructionWidth = DefaultConstructionWidth,
                            IgnoreConstructionWidth = IgnoreConstructionWidth,
                            SourceLayer = layer,
                            ZoneLayer = _zoneGroup.Layers.Where(l => l.StatusName == mapping.TargetStatus).First(),
                        };
                        layer.Zones.Add(zone);
                        Database.Attach(zone);
                    }
                    else
                    {
                        zone.Value = Value;
                        zone.AdditionalFilter = mapping.AdditionalFilter;
                        zone.DefaultConstructionWidth = DefaultConstructionWidth;
                        zone.IgnoreConstructionWidth = IgnoreConstructionWidth;
                        zone.SourceLayer = layer;
                    }
                }
            }

        }
    }
}