using FluentValidation;
using LayersIO.Connection;
using LayersIO.Model;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerDataViewModel
    {
        private readonly LayerData _layerData;
        private readonly LayersDatabaseContextSqlite _db;
        public LayerDataViewModel(LayerData layerData, LayersDatabaseContextSqlite context)
        {
            _db = context;
            _layerData = layerData;
            Prefix = layerData.Prefix;
            Separator = layerData.Separator;
            MainName = layerData.MainName;
            StatusName = layerData.StatusName;
            LayerProperties = new(layerData.LayerPropertiesData);
            LayerDrawTemplate = new(layerData.LayerDrawTemplateData);
            foreach (var zone in layerData.Zones)
            {
                Zones.Add(new(zone, _db));
            }
        }

        public string? Prefix { get; set; }

        public string? Separator;
        public string? MainName { get; set; }
        public string? StatusName { get; set; }

        public string Name
        {
            get
            {
                return string.Join(Separator, Prefix, MainName, StatusName);
            }
            set
            {
                string[] classifiers = value.Split(Separator);
                Prefix = classifiers[0];
                MainName = string.Join(Separator, classifiers.Skip(1).Take(classifiers.Length - 2).ToArray());
                StatusName = classifiers[^1];
            }
        }
        public LayerPropertiesViewModel LayerProperties { get; set; }
        public LayerDrawTemplateViewModel LayerDrawTemplate { get; set; }
        public ObservableCollection<ZoneInfoViewModel> Zones { get; set; } = new();

    }

    public class LayerDataViewModelValidator : AbstractValidator<LayerDataViewModel>
    {
        public LayerDataViewModelValidator()
        {
            RuleFor(l => l.Name).NotNull();
            RuleFor(l => l.Prefix).NotNull().Must(s => Regex.IsMatch(s!, @"^\w+$"));
            //RuleFor(l => l.StatusName).Must(s => NameParser.)
            
        }
    }

}