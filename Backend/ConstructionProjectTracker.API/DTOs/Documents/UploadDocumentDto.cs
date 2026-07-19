using Microsoft.AspNetCore.Http;

namespace ConstructionProjectTracker.API.DTOs.Documents;

public class UploadDocumentDto
{
    public int ProjectId { get; set; }
    public string Category { get; set; } = "Other";
    public IFormFile File { get; set; } = null!;
}
