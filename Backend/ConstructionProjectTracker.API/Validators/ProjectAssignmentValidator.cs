using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.ProjectAssignments;
using FluentValidation;

namespace ConstructionProjectTracker.API.Validators;

public class AssignEngineerValidator : AbstractValidator<AssignEngineerDto>
{
    public AssignEngineerValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("Project id is required.")
            .Must(projectId => context.Projects.Any(p => p.Id == projectId))
            .WithMessage("Project does not exist.");

        RuleFor(x => x.EngineerId)
            .GreaterThan(0).WithMessage("Engineer id is required.")
            .Must(engineerId => context.Engineers.Any(e => e.Id == engineerId))
            .WithMessage("Engineer does not exist.");

        RuleFor(x => x)
            .Must(dto => !context.ProjectAssignments.Any(
                pa => pa.ProjectId == dto.ProjectId && pa.EngineerId == dto.EngineerId))
            .WithMessage("This engineer is already assigned to the project.");
    }
}
