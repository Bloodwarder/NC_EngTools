using FluentValidation;
using LayersDatabaseEditor.ViewModel.Zones;

namespace LayersDatabaseEditor.Validation
{
    internal class SpecialZoneVmValidator : AbstractValidator<SpecialZoneVm>
    {
        public SpecialZoneVmValidator()
        {
            var validator = new SpecialZoneLayerVmValidator();
            RuleFor(z => z).Must(z => z.Value > 0 || z.AdditionalFilter == "Cancel").WithMessage("Для новой (не отменяемой) зоны полуширина буфера должна быть больше 0");
            RuleFor(z => z.SourceLayerVm).SetValidator(validator);
            RuleFor(z => z.ZoneLayerVm).SetValidator(validator);
        }
    }
}
