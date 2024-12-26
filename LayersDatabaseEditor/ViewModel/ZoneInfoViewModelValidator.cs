using FluentValidation;
using LayersDatabaseEditor.Utilities;

namespace LayersDatabaseEditor.ViewModel
{
    public class ZoneInfoViewModelValidator : AbstractValidator<ZoneInfoViewModel>
    {
        public ZoneInfoViewModelValidator()
        {
            RuleFor(z => z).Must(z => !z.IsActivated || z.Value > 0).WithMessage("Размер зоны должен быть больше 0");
            RuleFor(z => z.DefaultConstructionWidth).GreaterThan(0d).WithMessage("Ширина конструкции по умолчанию должна быть больше или равна 0");
        }
    }
}