using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Documents;
using ConstructionProjectTracker.API.Helpers;
using FluentValidation;

namespace ConstructionProjectTracker.API.Validators;

public class UploadDocumentValidator : AbstractValidator<UploadDocumentDto>
{
    public UploadDocumentValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("Project id is required.")
            .Must(projectId => context.Projects.Any(p => p.Id == projectId))
            .WithMessage("Project does not exist.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(DocumentCategoryRules.IsAllowedCategory)
            .WithMessage("Category is not valid.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.");

        RuleFor(x => x.File!.Length)
            .GreaterThan(0).WithMessage("File is required.")
            .When(x => x.File is not null);

        RuleFor(x => x.File!.Length)
            .LessThanOrEqualTo(DocumentFileRules.MaxFileSizeBytes)
            .WithMessage("File size must not exceed 20 MB.")
            .When(x => x.File is not null);

        RuleFor(x => x.File!)
            .Must(file =>
            {
                var extension = Path.GetExtension(file.FileName);
                return DocumentFileRules.IsAllowedExtension(extension);
            })
            .WithMessage("File type is not allowed. Allowed types: .pdf, .doc, .docx, .xls, .xlsx, .jpg, .jpeg, .png, .dwg")
            .When(x => x.File is not null);
    }
}
