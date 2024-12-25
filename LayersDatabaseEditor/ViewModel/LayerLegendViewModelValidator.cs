using FluentValidation;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerLegendViewModelValidator : AbstractValidator<LayerLegendViewModel>
    {
        public LayerLegendViewModelValidator()
        {
            RuleFor(l => l.Label).NotNull().NotEmpty();
        }
    }
}