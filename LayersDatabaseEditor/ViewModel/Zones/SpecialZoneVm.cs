using LayersDatabaseEditor.Validation;
using LayersIO.Connection;
using LayersIO.Model;
using LoaderCore.SharedData;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LayersDatabaseEditor.ViewModel.Zones
{
    public class SpecialZoneVm : IDbRelatedViewModel, INotifyPropertyChanged
    {
        private SpecialZoneLayerVm _sourceLayerVm = null!;
        private SpecialZoneLayerVm _zoneLayerVm = null!;
        private double _value;
        private double _defaultConstructionWidth;
        private string? _additionalFilter;
        private static readonly SpecialZoneVmValidator _validator = new();

        private static RelayCommand? _cancelZoneCommand;


        public event PropertyChangedEventHandler? PropertyChanged;

        public SpecialZoneVm(SpecialZoneLayerVm source, LayersDatabaseContextSqlite context)
        {
            Database = context;
            _sourceLayerVm = source;
            _sourceLayerVm.PropertyChanged += OnSourcePrefixChanged;
            _zoneLayerVm = new SpecialZoneLayerVm();
            _zoneLayerVm.PropertyChanged += OnZonePrefixChanged;
            ZoneInfos = Array.Empty<ZoneInfoData>();
        }

        public SpecialZoneVm(ZoneInfoData zoneInfo, LayersDatabaseContextSqlite context)
        {
            Database = context;
            ZoneInfos = new ZoneInfoData[] { zoneInfo };
            ResetValues();
        }

        public SpecialZoneVm(IEnumerable<ZoneInfoData> fullGroupZoneInfos, LayersDatabaseContextSqlite context)
        {
            Database = context;
            ZoneInfos = fullGroupZoneInfos;

            ResetValues();
        }

        public RelayCommand CancelZoneCommand
        {
            get => _cancelZoneCommand ??= new(obj => CancelZone(obj), obj => true);
            set => _cancelZoneCommand = value;
        }

        private static void CancelZone(object? obj)
        {
            if (obj is not SpecialZoneVm zoneVm)
                return;
            if (zoneVm!.AdditionalFilter?.EndsWith("Cancel") ?? false)
                zoneVm.AdditionalFilter = Regex.Replace(zoneVm.AdditionalFilter, @";?Cancel", string.Empty);
            else
            {
                zoneVm.AdditionalFilter = string.IsNullOrEmpty(zoneVm.AdditionalFilter) ? "Cancel" : string.Join(";", zoneVm.AdditionalFilter, "Cancel");
            }
        }

        public SpecialZoneLayerVm SourceLayerVm
        {
            get => _sourceLayerVm;
            set
            {
                _sourceLayerVm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }


        public SpecialZoneLayerVm ZoneLayerVm
        {
            get => _zoneLayerVm;
            set
            {
                _zoneLayerVm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }

        public IEnumerable<string>? AvailableSourceStatuses => SpecialZoneLayerVm.GetStatusesAddAll(SourceLayerVm.Prefix);

        public IEnumerable<string>? AvailableZoneStatuses => SpecialZoneLayerVm.GetStatuses(ZoneLayerVm?.Prefix);

        internal LayersDatabaseContextSqlite Database { get; }

        public IEnumerable<ZoneInfoData> ZoneInfos { get; private set; }

        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }
        public double DefaultConstructionWidth
        {
            get => _defaultConstructionWidth;
            set
            {
                _defaultConstructionWidth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }
        public string? AdditionalFilter
        {
            get => _additionalFilter;
            set
            {
                _additionalFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            }
        }

        public bool IsValid => _validator.Validate(this).IsValid;

        public bool IsUpdated => ZoneLayerVm.MainName != ZoneLayerVm.LayerGroup?.MainName
                                 || ZoneLayerVm.Prefix != ZoneLayerVm.LayerGroup?.Prefix
                                 || Value != ZoneInfos?.First().Value
                                 || AdditionalFilter != ZoneInfos.First().AdditionalFilter
                                 || DefaultConstructionWidth != ZoneInfos?.First().DefaultConstructionWidth;

        public void ResetValues()
        {
            if (!ZoneInfos.Any())
            {
                return; // Удалится из коллекции на следующем уровне. Прямой вызов в UI не предусмотрен
            }
            else if (ZoneInfos.Count() == 1)
            {
                var first = ZoneInfos.First();

                SourceLayerVm = new SpecialZoneLayerVm(first.SourceLayer);
                SourceLayerVm.PropertyChanged += OnSourcePrefixChanged;

                ZoneLayerVm = new SpecialZoneLayerVm(first.ZoneLayer);
                ZoneLayerVm.PropertyChanged += OnZonePrefixChanged;

                Value = first.Value;
                DefaultConstructionWidth = first.DefaultConstructionWidth;
                AdditionalFilter = first.AdditionalFilter;
            }
            else
            {
                var first = ZoneInfos.First();

                SourceLayerVm = new SpecialZoneLayerVm(first.SourceLayer.LayerGroup); // Передаётся группа, а не слой, чтобы выбрать все статусы
                SourceLayerVm.PropertyChanged += OnSourcePrefixChanged;

                ZoneLayerVm = new SpecialZoneLayerVm(first.ZoneLayer);
                ZoneLayerVm.PropertyChanged += OnZonePrefixChanged;

                Value = first.Value;
                DefaultConstructionWidth = first.DefaultConstructionWidth;
                AdditionalFilter = first.AdditionalFilter;
            }
        }

        public void UpdateDatabaseEntities()
        {
            if (!IsValid)
            {
                return;
            }

            if (ZoneLayerVm.LayerGroup == null || (ZoneInfos.Any() && ZoneLayerVm.MainName != ZoneLayerVm.LayerGroup.MainName))
            {
                Database.Zones.RemoveRange(ZoneInfos);
                ZoneLayerVm.RequeryLayerGroup(Database);
            }

            if (SourceLayerVm.Status == SpecialZoneLayerVm.AllStatusesString)
            {
                Func<LayerGroupData, string, string, bool> layerGroupIndexQuery = (g, pr, mn) => g.Prefix == pr && g.MainName == mn;
                var group = SourceLayerVm.LayerGroup ??
                    Database.LayerGroups.Single(g => layerGroupIndexQuery(g, SourceLayerVm.Prefix!, SourceLayerVm.MainName!)); // ! - должно было перед этим пройти валидацию
                List<ZoneInfoData> list = new();
                foreach (var layer in group.Layers)
                {
                    var zoneInfo = layer.Zones.FirstOrDefault(z => z.ZoneLayer == ZoneLayerVm.Layer! && z.IsSpecial);
                    if (zoneInfo == null)
                    {
                        zoneInfo = new ZoneInfoData()
                        {
                            SourceLayer = layer,
                            ZoneLayer = ZoneLayerVm.Layer!
                        };
                        layer.Zones.Add(zoneInfo);
                    }
                    zoneInfo.DefaultConstructionWidth = DefaultConstructionWidth;
                    zoneInfo.AdditionalFilter = AdditionalFilter;
                    zoneInfo.Value = Value;
                    zoneInfo.IsSpecial = true;
                    list.Add(zoneInfo);
                }
                ZoneInfos = list;
            }
            else
            {
                List<ZoneInfoData> zoneInfos = Database.Zones.Where(z => z.IsSpecial
                                                                         && z.SourceLayer.LayerGroup == SourceLayerVm.LayerGroup
                                                                         && z.ZoneLayer == ZoneLayerVm.Layer
                                                                         && z.AdditionalFilter == AdditionalFilter)
                                                             .ToList();

                ZoneInfoData? zoneInfo = zoneInfos.FirstOrDefault(z => z.SourceLayer.StatusName == SourceLayerVm.Status);
                if (zoneInfo != null)
                    zoneInfos.Remove(zoneInfo);
                else
                {
                    zoneInfo = new ZoneInfoData()
                    {
                        SourceLayer = SourceLayerVm.Layer!,
                        ZoneLayer = ZoneLayerVm.Layer!
                    };
                    Database.Zones.Add(zoneInfo);
                }

                Database.Zones.RemoveRange(zoneInfos);

                zoneInfo.DefaultConstructionWidth = DefaultConstructionWidth;
                zoneInfo.AdditionalFilter = AdditionalFilter;
                zoneInfo.Value = Value;
                zoneInfo.IsSpecial = true;
                ZoneInfos = new ZoneInfoData[] { zoneInfo };
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSourcePrefixChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SpecialZoneLayerVm.Prefix))
                OnPropertyChanged(nameof(AvailableSourceStatuses));
        }

        private void OnZonePrefixChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SpecialZoneLayerVm.Prefix))
                OnPropertyChanged(nameof(AvailableZoneStatuses));
        }
    }
}
