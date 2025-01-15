using FluentValidation;
using LayersDatabaseEditor.Utilities;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerPropertiesViewModelValidator : AbstractValidator<LayerPropertiesViewModel>
    {
        HashSet<string> _availableLinetypes;
        public LayerPropertiesViewModelValidator()
        {
            RuleFor(p => p.ConstantWidth).GreaterThanOrEqualTo(0d).WithMessage("Глобальная ширина должна быть больше или равна 0");
            RuleFor(p => p.LinetypeScale).GreaterThanOrEqualTo(0d).WithMessage("Масштаб типа линий должен быть больше или равен 0");
            _availableLinetypes = LinetypeProvider.GetLinetypes().ToHashSet();
            RuleFor(p => p.LinetypeName).NotNull().Must(n => _availableLinetypes.Contains(n!)).WithMessage("Неопознанный тип линий");
            RuleFor(p => p.LineWeight).Must(lw => lw >= 0 || lw == -3).WithMessage("Вес линий должен быть больше или равен 0 или равен -3 (по умолчанию)");
        }
    }
}