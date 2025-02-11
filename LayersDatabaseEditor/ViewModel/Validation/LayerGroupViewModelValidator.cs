using FluentValidation;
using NameClassifiers;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerGroupViewModelValidator : AbstractValidator<LayerGroupVm>
    {
        public LayerGroupViewModelValidator()
        {
            RuleFor(l => l.Prefix).NotNull().NotEmpty().Must(p => NameParser.LoadedParsers.ContainsKey(p!)).WithMessage("Парсер с указанным префиксом должен быть загружен");
            RuleFor(l => l.LayerLegend).SetValidator(new LayerLegendViewModelValidator());
            // TODO: проверка наличия AlterLayer в списке существующих слоёв
        }
    }
}