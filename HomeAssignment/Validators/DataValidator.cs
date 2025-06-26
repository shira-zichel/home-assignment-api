using FluentValidation;
using HomeAssignment.DTOs;


namespace HomeAssignment.Validators
{
    public class CreateDataItemValidator : AbstractValidator<CreateDataItemDto>
    {
        public CreateDataItemValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .Length(1, 500).WithMessage("Value must be between 1 and 500 characters.");
        }
    }

    public class UpdateDataItemValidator : AbstractValidator<UpdateDataItemDto>
    {
        public UpdateDataItemValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .Length(1, 500).WithMessage("Value must be between 1 and 500 characters.");
        }
    }

    public class IdValidator : AbstractValidator<int>
    {
        public IdValidator()
        {
            RuleFor(x => x)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
