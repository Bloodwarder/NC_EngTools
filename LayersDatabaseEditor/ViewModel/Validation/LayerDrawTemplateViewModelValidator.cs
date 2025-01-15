using FluentValidation;
using LayersIO.DataTransfer;
using System.IO;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerDrawTemplateViewModelValidator : AbstractValidator<LayerDrawTemplateViewModel>
    {
        public LayerDrawTemplateViewModelValidator()
        {
            RuleFor(l => l.DrawTemplate).Cascade(CascadeMode.Stop).NotNull().IsInEnum().Must(d => d != DrawTemplate.Undefined);

            RuleFor(l => l.InnerHatchBrightness).InclusiveBetween(-1d, 1d).WithMessage("Сдвиг яркости InnerHatchBrightness должен быть в пределах -1 и 1");
            RuleFor(l => l.InnerBorderBrightness).InclusiveBetween(-1d, 1d).WithMessage("Сдвиг яркости InnerBorderBrightness должен быть в пределах -1 и 1");
            RuleFor(l => l.OuterHatchBrightness).InclusiveBetween(-1d, 1d).WithMessage("Сдвиг яркости OuterHatchBrightness должен быть в пределах -1 и 1");
            RuleFor(l => l.InnerHatchBrightness).InclusiveBetween(-1d, 1d).WithMessage("Сдвиг яркости InnerHatchBrightness должен быть в пределах -1 и 1");

            RuleFor(l => l.InnerHatchScale).GreaterThan(0).WithMessage("Масштаб штриховки InnerHatchScale должен быть больше нуля");
            RuleFor(l => l.OuterHatchScale).GreaterThan(0).WithMessage("Масштаб штриховки OuterHatchScale должен быть больше нуля");

            RuleFor(l => l.InnerHatchAngle).InclusiveBetween(0d, 360d).WithMessage("Угол внутренней штриховки должен быть в пределах 0 и 360");
            RuleFor(l => l.OuterHatchAngle).InclusiveBetween(0d, 360d).WithMessage("Угол внешней штриховки должен быть в пределах 0 и 360");

            Func<string?, bool> widthParsePredicate = s => s is null
                                                    || double.TryParse(s, out double v) && v > 0
                                                    || s.EndsWith(@"*") && double.TryParse(s.Replace("*", ""), out var value) && value is > 0 and <= 1;

            RuleFor(l => l.Width).Must(widthParsePredicate)
                .WithMessage("Длина условного знака должна иметь быть положительным числом или иметь формат n*, где n больше 0 и меньше или равно 1");
            RuleFor(l => l.Height).Must(widthParsePredicate)
                .WithMessage("Ширина условного знака должна иметь быть положительным числом или иметь формат n*, где n больше 0 и меньше или равно 1");
            RuleFor(l => l.FenceWidth).Must(widthParsePredicate)
                .WithMessage("Длина условного знака ограждения должна иметь быть положительным числом или иметь формат n*, где n больше 0 и меньше или равно 1");
            RuleFor(l => l.FenceHeight).Must(widthParsePredicate)
                .WithMessage("Ширина условного знака ограждения должна иметь быть положительным числом или иметь формат n*, где n больше 0 и меньше или равно 1");

            RuleFor(l => l).Must(l => l.DrawTemplate != DrawTemplate.BlockReference && string.IsNullOrEmpty(l.BlockPath)
                                      || File.Exists(l.BlockPath) && l.BlockPath.EndsWith(".dwg"))
                           .WithMessage("Полный путь к файлу блока должен вести к существующему .dwg файлу");

            Func<LayerDrawTemplateViewModel, bool> dbPredicate =
                l => string.IsNullOrEmpty(l.FenceLayer) || l.Database.Layers.Any(lyr => l.FenceLayer.StartsWith(lyr.Prefix) && l.FenceLayer.EndsWith(lyr.MainName));
            RuleFor(l => l).Must(dbPredicate);
        }
    }
}