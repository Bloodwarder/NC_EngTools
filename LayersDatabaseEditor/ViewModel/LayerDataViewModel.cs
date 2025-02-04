using FluentValidation.Results;
using LayersDatabaseEditor.ViewModel.Validation;
using LayersDatabaseEditor.ViewModel.Zones;
using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerDataViewModel : INotifyPropertyChanged, IDbRelatedViewModel
    {
        private readonly LayerData _layerData;
        private readonly LayersDatabaseContextSqlite _db;
        private readonly LayerGroupViewModel _parentGroup;
        private string _errors = "";
        private bool _isValid;
        private string? _statusName = null!;
        private LayerPropertiesViewModel _layerProperties = null!;
        private LayerDrawTemplateViewModel _layerDrawTemplate = null!;

        public LayerDataViewModel(LayerGroupViewModel parentGroup, LayerData layerData, LayersDatabaseContextSqlite context)
        {
            _parentGroup = parentGroup;
            _db = context;
            _layerData = layerData;
            _parentGroup.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LayerGroupViewModel.MainName) || e.PropertyName == nameof(LayerGroupViewModel.Prefix))
                {
                    OnPropertyChanged(nameof(IsValid));
                    OnPropertyChanged(nameof(IsUpdated));
                }
            };
            ResetValues();
        }
        internal static LayerDataViewModelValidator Validator { get; } = new();
        internal LayersDatabaseContextSqlite Database => _db;
        public bool IsValid
        {
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged();
            }
        }
        public string Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                OnPropertyChanged();
            }
        }

        public bool IsUpdated
        {
            get
            {
                bool propsUpdated = LayerProperties.IsUpdated();
                bool drawUpdated = LayerDrawTemplate.IsUpdated();
                bool statusUpdated = StatusName != _layerData.StatusName;
                return propsUpdated || drawUpdated || statusUpdated;
            }
        }


        public string? StatusName
        {
            get => _statusName;
            set
            {
                _statusName = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return string.Join(_parentGroup.Separator, _parentGroup.Prefix, _parentGroup.MainName, StatusName);
            }
        }
        public LayerPropertiesViewModel LayerProperties
        {
            get => _layerProperties;
            set
            {
                _layerProperties = value;
                OnPropertyChanged();
            }
        }
        public LayerDrawTemplateViewModel LayerDrawTemplate
        {
            get => _layerDrawTemplate;
            set
            {
                _layerDrawTemplate = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ZoneGroupInfoViewModel> Zones { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ResetValues()
        {
            StatusName = _layerData.StatusName;

            if (LayerProperties == null)
            {
                LayerProperties = new(_layerData.LayerPropertiesData, _db);
                LayerProperties.PropertyChanged += (s, e) => OnPropertyChanged(nameof(IsUpdated));
            }
            else
                LayerProperties.ResetValues();

            if (LayerDrawTemplate == null)
            {
                LayerDrawTemplate = new(_layerData.LayerDrawTemplateData, _db);
                LayerDrawTemplate.PropertyChanged += (s, e) => OnPropertyChanged(nameof(IsUpdated));
            }
            else
                LayerDrawTemplate.ResetValues();

            //foreach (var zone in _layerData.Zones)
            //{
            //    Zones.Add(new(zone, _db));
            //}
            //OnPropertyChanged(nameof(IsUpdated));
        }

        public bool Validate()
        {
            ValidationResult validationResult = Validator.Validate(this);
            if (!validationResult.IsValid)
            {
                StringBuilder stringBuilder = new($"Слой {this.Name} - ошибки:\n");
                var errors = validationResult.Errors.Select(e => $"{e.ErrorMessage}. Неверное значение: {e.AttemptedValue}").AsEnumerable();
                foreach (var error in errors)
                    stringBuilder.AppendLine(error);
                Errors = stringBuilder.ToString();
            }
            else
            {
                Errors = string.Empty;
            }
            this.IsValid = validationResult.IsValid;
            return validationResult.IsValid;
        }

        public void UpdateDatabaseEntities()
        {
            var layerEntityState = Database.Entry(_layerData).State;
            var layerGroupEntityState = Database.Entry(_layerData.LayerGroup).State;

            if (layerEntityState == EntityState.Detached && layerGroupEntityState == EntityState.Detached)
            {
                System.Windows.MessageBox.Show("Группа слоёв не сохранена. Сначала сохраните группу",
                                               "Несохранённая группа слоёв",
                                               System.Windows.MessageBoxButton.OK,
                                               System.Windows.MessageBoxImage.Warning);
                // TODO: запись в лог
                return;
            }

            if (this.Validate())
            {
                _layerData.StatusName = StatusName!;
                LayerProperties.UpdateDbEntity();
                LayerDrawTemplate.UpdateDbEntity();
                var state = Database.Entry(_layerData).State;
                if (state == EntityState.Detached) // запрос был с трекингом, соответственно Detached только у новых сущностей
                {
                    Database.Layers.Add(_layerData);
                }
                OnPropertyChanged(nameof(IsUpdated));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName != nameof(Errors) && propertyName != nameof(IsValid))
            {
                Validate();
                if (propertyName != nameof(IsUpdated))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUpdated)));
            }
        }
    }
}