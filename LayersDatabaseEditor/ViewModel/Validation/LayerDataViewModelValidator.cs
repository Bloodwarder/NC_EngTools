using FluentValidation;
using NameClassifiers;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class LayerDataViewModelValidator : AbstractValidator<LayerDataViewModel>
    {
        public LayerDataViewModelValidator()
        {
            RuleFor(l => l.Name).NotNull().Must(n => NameParser.ParseLayerName(n).Status == LayerInfoParseStatus.Success)
                .WithMessage(n => $"Парсер не может распознать имя слоя {n}");
            RuleFor(l => l.LayerProperties).SetValidator(new LayerPropertiesViewModelValidator());
            RuleFor(l => l.LayerDrawTemplate).SetValidator(new LayerDrawTemplateViewModelValidator());
        }
    }

}