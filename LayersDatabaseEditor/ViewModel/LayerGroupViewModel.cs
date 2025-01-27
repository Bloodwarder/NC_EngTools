using LayersDatabaseEditor.ViewModel.Validation;
using LayersIO.Connection;
using LayersIO.Model;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerGroupViewModel : INotifyPropertyChanged, INotifyPropertyChanging, IDbRelatedViewModel
    {
        private readonly LayerGroupData _layerGroupData;
        private readonly LayersDatabaseContextSqlite _db;
        private readonly NameParser _parser;
        private string? _prefix;
        private bool _isValid;
        private string? _mainName;
        private string _separator;
        private string? _alternateLayer;
        private string _errors = "";
        private LayerLegendViewModel _layerLegend = null!;

        public LayerGroupViewModel(string mainName, LayersDatabaseContextSqlite context)
        {
            _db = context;
            string prefix = NameParser.GetPrefix(mainName) ?? throw new ArgumentNullException("Префикс не найден");
            _parser = NameParser.LoadedParsers[prefix];
            LayerGroupData layerGroupData = new()
            {
                Prefix = prefix,
                MainName = mainName.Replace($"{prefix}{_parser.Separator}", string.Empty),
                Separator = _parser.Separator,
                LayerLegendData = new(),
            };
            _layerGroupData = layerGroupData;
            _separator = _parser.Separator;

            ResetValues();

            foreach (var status in _parser.GetStatusArray())
            {
                Layers.Add(LayerDataViewModelFactory.Create(this, layerGroupData, status));
            }
        }
        public LayerGroupViewModel(LayerGroupData layerGroupData, LayersDatabaseContextSqlite context)
        {
            _parser = NameParser.LoadedParsers[layerGroupData.Prefix];
            _db = context;
            _layerGroupData = layerGroupData;
            _separator = _parser.Separator;
            ResetValues();
        }
        internal static LayerGroupViewModelValidator Validator { get; private set; } = new();

        internal LayersDatabaseContextSqlite Database => _db;
        public string Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                OnPropertyChanged();
            }
        }
        public bool IsValid
        {
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged();
            }
        }

        public bool IsUpdated
        {
            get
            {
                bool legendUpdated = LayerLegend.IsUpdated();
                bool valuesUpdated = Prefix != _layerGroupData.Prefix
                                     || MainName != _layerGroupData.MainName
                                     || Separator != _layerGroupData.Separator
                                     || (AlternateLayer == null && _layerGroupData.AlternateLayer != null)
                                     || (AlternateLayer != null && AlternateLayer != $"{_parser.Prefix}{_parser.Separator}{_layerGroupData.AlternateLayer}");
                bool layersUpdated = Layers.Any(l => l.IsUpdated);
                return valuesUpdated || legendUpdated || layersUpdated;
            }
        }

        public int Id => _layerGroupData.Id;

        public string? Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix != value)
                {
                    OnPropertyChanging();
                    _prefix = value;
                    OnPropertyChanged();
                }
            }
        }


        public string? MainName
        {
            get => _mainName;
            set
            {
                if (_mainName != value)
                {
                    OnPropertyChanging();
                    _mainName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Separator
        {
            get => _separator;
            set
            {
                _separator = value;
                OnPropertyChanged();
            }
        }
        public string Name
        {
            get
            {
                return string.Join(Separator, Prefix, MainName);
            }
            set
            {
                string[] classifiers = value.Split(Separator);
                Prefix = classifiers[0];
                MainName = string.Join(Separator, classifiers.Skip(1).Take(classifiers.Length - 1).ToArray());
            }
        }

        public string? AlternateLayer
        {
            get => _alternateLayer;
            set
            {
                _alternateLayer = value;
                OnPropertyChanged();
            }
        }
        public LayerLegendViewModel LayerLegend
        {
            get => _layerLegend;
            set
            {
                _layerLegend = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<LayerDataViewModel> Layers { get; } = new();


        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;

        internal bool Validate()
        {
            var groupValidationResult = Validator.Validate(this);
            bool isValidResult = groupValidationResult.IsValid && Layers.All(l => l.IsValid);
            if (!isValidResult)
            {
                StringBuilder stringBuilder = new($"Группа {this.Name} - ошибки:\n");
                var errors = groupValidationResult.Errors.Select(e => $"{e.ErrorMessage}. Неверное значение: {e.AttemptedValue}").AsEnumerable();
                foreach (var error in errors)
                    stringBuilder.AppendLine(error);
                foreach (var layer in Layers)
                {
                    if (!layer.IsValid)
                        stringBuilder.AppendLine(layer.Errors);
                }
                Errors = stringBuilder.ToString();
            }
            else
            {
                Errors = string.Empty;
            }
            this.IsValid = isValidResult;
            return groupValidationResult.IsValid;
        }


        public void UpdateDatabaseEntities()
        {
            if (this.IsValid)
            {
                _layerGroupData.Prefix = Prefix!;
                if (AlternateLayer == string.Empty)
                {
                    _layerGroupData.AlternateLayer = null;
                }
                else
                {
                    _layerGroupData.AlternateLayer = AlternateLayer?.Replace($"{_parser.Prefix}{_parser.Separator}", string.Empty);
                }
                var state = Database.Entry(_layerGroupData).State;
                if (state == EntityState.Detached)
                {
                    // UNDONE: НЕДОРАБОТАНО. Пока просто не пересоздаём слои, но надо доработать. Возможно перезаписать удаляемую сущность.
                    // Проверка на случай "пересоздания" группы. Открепить от контекста удалённые сущности.
                    var deletedEntities = Database.ChangeTracker.Entries<LayerGroupData>()
                                                                .Where(e => e.State == EntityState.Deleted
                                                                            && e.Entity.Prefix == Prefix
                                                                            && e.Entity.MainName == MainName)
                                                                .AsEnumerable();
                    foreach (var entity in deletedEntities)
                        entity.State = EntityState.Detached;
                    Database.LayerGroups.Add(_layerGroupData);
                }

                LayerLegend.UpdateDbEntity();
                foreach (var layer in Layers)
                {
                    layer.UpdateDatabaseEntities();
                }
                OnPropertyChanged(nameof(IsUpdated));
            }
        }

        public void ResetValues()
        {
            Prefix = _layerGroupData.Prefix;
            MainName = _layerGroupData.MainName;
            Separator = _layerGroupData.Separator;
            AlternateLayer = _layerGroupData.AlternateLayer != null ? $"{_parser.Prefix}{_parser.Separator}{_layerGroupData.AlternateLayer}" : null;

            if (LayerLegend == null)
            {
                LayerLegend = new(_layerGroupData.LayerLegendData);
                LayerLegend.PropertyChanged += (s, e) => OnPropertyChanged(nameof(IsUpdated));
            }
            else
                LayerLegend.ResetValues();

            foreach (var layer in _layerGroupData.Layers)
            {
                var layerModel = Layers.FirstOrDefault(lm => lm.Name == layer.Name);
                if (layerModel != null)
                {
                    layerModel.ResetValues();
                }
                else
                {
                    layerModel = new LayerDataViewModel(this, layer, _db);
                    layerModel.PropertyChanged += (s, e) => OnPropertyChanged(nameof(IsUpdated));
                    Layers.Add(layerModel);
                }
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

        private void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
    }
}