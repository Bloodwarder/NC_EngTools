﻿using FluentValidation;
using LayersDatabaseEditor.ViewModel.Zones;

namespace LayersDatabaseEditor.ViewModel.Validation
{
    public class ZoneInfoViewModelValidator : AbstractValidator<ZoneGroupInfoVm>
    {
        public ZoneInfoViewModelValidator()
        {
            RuleFor(z => z).Must(z => !z.IsActivated || z.Value > 0).WithMessage("Размер зоны должен быть больше 0");
            RuleFor(z => z.DefaultConstructionWidth).GreaterThanOrEqualTo(0d).WithMessage("Ширина конструкции по умолчанию должна быть больше или равна 0");
        }
    }
}