using FluentValidation;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerLegendViewModelValidator : AbstractValidator<LayerLegendViewModel>
    {
        public LayerLegendViewModelValidator()
        {
            RuleFor(l => l.Label).NotNull().NotEmpty();
        }
    }
}