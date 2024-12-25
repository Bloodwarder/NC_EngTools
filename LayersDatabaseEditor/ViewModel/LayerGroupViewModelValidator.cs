using FluentValidation;
using NameClassifiers;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerGroupViewModelValidator : AbstractValidator<LayerGroupViewModel>
    {
        public LayerGroupViewModelValidator()
        {
            RuleFor(l => l.Prefix).NotNull().NotEmpty().Must(p => NameParser.LoadedParsers.ContainsKey(p!)).WithMessage("Парсер с указанным префиксом должен быть загружен");
            RuleFor(l => l.LayerLegend).SetValidator(new LayerLegendViewModelValidator());
            //RuleForEach(lg => lg.Layers).SetValidator( new LayerDataViewModelValidator());
        }
    }
}