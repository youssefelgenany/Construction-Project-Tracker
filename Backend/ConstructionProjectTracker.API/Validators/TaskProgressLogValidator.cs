using ConstructionProjectTracker.API.DTOs.Tasks;
using FluentValidation;

namespace ConstructionProjectTracker.API.Validators;

public class CreateTaskProgressLogValidator : AbstractValidator<CreateTaskProgressLogDto>
{
    public CreateTaskProgressLogValidator()
    {
        RuleFor(x => x.NewProgress)
            .InclusiveBetween(0, 90)
            .WithMessage("Progress must be between 0 and 90.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A description is required when updating progress.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
    }
}
