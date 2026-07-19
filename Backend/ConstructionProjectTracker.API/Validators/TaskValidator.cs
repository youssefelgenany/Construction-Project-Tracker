using ConstructionProjectTracker.API.Data;

using ConstructionProjectTracker.API.DTOs.Tasks;

using ConstructionProjectTracker.API.Enums;

using FluentValidation;

using Microsoft.EntityFrameworkCore;



namespace ConstructionProjectTracker.API.Validators;



public class CreateTaskValidator : AbstractValidator<CreateTaskDto>

{

    public CreateTaskValidator(ApplicationDbContext context)

    {

        RuleFor(x => x.Title)

            .NotEmpty().WithMessage("Task title is required.")

            .MaximumLength(200).WithMessage("Task title must not exceed 200 characters.");



        RuleFor(x => x.Priority)

            .IsInEnum().WithMessage("Priority is required and must be a valid value.");



        RuleFor(x => x.ProjectId)

            .GreaterThan(0).WithMessage("Project id is required.")

            .Must(projectId => context.Projects.Any(p => p.Id == projectId))

            .WithMessage("Project does not exist.");



        RuleFor(x => x.AssignedEngineerId)

            .GreaterThan(0).WithMessage("Assigned engineer id is required.")

            .Must(engineerId => context.Engineers.Any(e => e.Id == engineerId))

            .WithMessage("Engineer does not exist.");



        RuleFor(x => x)

            .Must(dto => context.ProjectAssignments.Any(

                pa => pa.ProjectId == dto.ProjectId && pa.EngineerId == dto.AssignedEngineerId))

            .WithMessage("Engineer is not assigned to this project.");



        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Due date must be on or after the start date.");
    }
}

public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority is required and must be a valid value.");

        RuleFor(x => x.CompletionPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Completion percentage must be between 0 and 100.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid value.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Due date must be on or after the start date.");
    }
}
