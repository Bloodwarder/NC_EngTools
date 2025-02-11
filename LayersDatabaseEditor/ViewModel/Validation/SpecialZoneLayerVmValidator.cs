using FluentValidation;
using LayersDatabaseEditor.ViewModel.Zones;
using NameClassifiers;

namespace LayersDatabaseEditor.Validation
{
    internal class SpecialZoneLayerVmValidator : AbstractValidator<SpecialZoneLayerVm>
    {
        public SpecialZoneLayerVmValidator() 
        {
            RuleFor(z => z.Prefix).NotNull().NotEmpty().Must(p => NameParser.LoadedParsers.ContainsKey(p ?? string.Empty));
            RuleFor(z => z.MainName).NotNull().NotEmpty();
            RuleFor(z => z.Status).NotNull().NotEmpty();
        }
    }
}
