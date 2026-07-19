using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Engineers;
using FluentValidation;

namespace ConstructionProjectTracker.API.Validators;

public class CreateEngineerValidator : AbstractValidator<CreateEngineerDto>
{
    public CreateEngineerValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .Must(email => !context.Users.Any(u => u.Email == email))
            .WithMessage("Email is already in use.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required.");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.");
    }
}

public class UpdateEngineerValidator : AbstractValidator<UpdateEngineerDto>
{
    public UpdateEngineerValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required.");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.");
    }
}
