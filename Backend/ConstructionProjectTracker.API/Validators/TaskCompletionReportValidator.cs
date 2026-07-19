using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using FluentValidation;

namespace ConstructionProjectTracker.API.Validators;

public class RejectCompletionReportValidator : AbstractValidator<RejectCompletionReportDto>
{
    public RejectCompletionReportValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("A rejection comment is required.")
            .MaximumLength(1000).WithMessage("Rejection comment must not exceed 1000 characters.");
    }
}
