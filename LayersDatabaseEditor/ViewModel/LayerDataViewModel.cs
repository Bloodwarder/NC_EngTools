using FluentValidation.Results;
using LayersIO.Connection;
using LayersIO.Model;
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
        private bool _isUpdated;
        private string _errors = "";
        private bool _isValid;
        private string? _statusName = null!;

        public LayerDataViewModel(LayerGroupViewModel parentGroup, LayerData layerData, LayersDatabaseContextSqlite context)
        {
            _parentGroup = parentGroup;
            _db = context;
            _layerData = layerData;
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
        public LayerPropertiesViewModel LayerProperties { get; set; } = null!;
        public LayerDrawTemplateViewModel LayerDrawTemplate { get; set; } = null!;
        public ObservableCollection<ZoneInfoViewModel> Zones { get; set; } = new();

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

            foreach (var zone in _layerData.Zones)
            {
                Zones.Add(new(zone, _db));
            }
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

        public void UpdateDbEntity()
        {

            if (this.Validate())
            {
                _db.Attach(_layerData);
                _layerData.StatusName = StatusName!;
                LayerProperties.UpdateDbEntity();
                LayerDrawTemplate.UpdateDbEntity();
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