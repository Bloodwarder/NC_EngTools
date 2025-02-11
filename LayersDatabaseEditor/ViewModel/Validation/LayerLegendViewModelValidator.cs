using FluentValidation;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerLegendViewModelValidator : AbstractValidator<LayerLegendVm>
    {
        public LayerLegendViewModelValidator()
        {
            RuleFor(l => l.Label).NotNull().NotEmpty();
        }
    }
}