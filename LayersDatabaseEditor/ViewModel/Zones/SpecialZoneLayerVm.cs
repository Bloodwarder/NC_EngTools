using LayersDatabaseEditor.Validation;
using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using NameClassifiers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel.Zones
{
    public class SpecialZoneLayerVm : INotifyPropertyChanged, ICloneable
    {
        internal const string AllStatusesString = "Все статусы";

        private string? _prefix;
        private string? _mainName;
        private string? _status;
        private LayerGroupData? _layerGroupData;

        public SpecialZoneLayerVm()
        {
            _prefix = string.Empty;
            _mainName = string.Empty;
            _status = string.Empty;
        }

        public SpecialZoneLayerVm(LayerData layer)
        {
            _prefix = layer.Prefix;
            _mainName = layer.MainName;
            _status = layer.StatusName;
            _layerGroupData = layer.LayerGroup;
            InitialStatus = _status;
        }

        public SpecialZoneLayerVm(LayerGroupData layerGroup)
        {
            _prefix = layerGroup.Prefix;
            _mainName = layerGroup.MainName;
            _status = AllStatusesString;
            _layerGroupData = layerGroup;
            InitialStatus = _status;
        }


        public string? Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                OnPropertyChanged();
            }
        }
        public string? MainName
        {
            get => _mainName;
            set
            {
                if (value != _mainName)
                {
                    _mainName = value;
                    OnPropertyChanged();
                }
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

        public LayerGroupData? LayerGroup => _layerGroupData;

        internal string? InitialStatus { get; set; }

        internal LayerData? Layer =>
            Status != AllStatusesString ?
            _layerGroupData?.Layers.First(l => l.StatusName == Status) :
            null;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static IEnumerable<string> GetStatusesAddAll(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return Array.Empty<string>();
            var statuses = new List<string>() { AllStatusesString };
            statuses.AddRange(NameParser.LoadedParsers[prefix].GetStatusArray());
            return statuses;
        }
        public static IEnumerable<string> GetStatuses(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return Array.Empty<string>();
            return NameParser.LoadedParsers[prefix].GetStatusArray();
        }

        public static IEnumerable<string> GetPrefixes()
        {
            return NameParser.LoadedParsers.Keys;
        }

        internal void RequeryLayerGroup(LayersDatabaseContextSqlite context)
        {
            _layerGroupData = context.LayerGroups.Where(g => g.Prefix == Prefix && g.MainName == MainName).Include(g => g.Layers).Single();
        }

        public object Clone() => MemberwiseClone();
    }


}
