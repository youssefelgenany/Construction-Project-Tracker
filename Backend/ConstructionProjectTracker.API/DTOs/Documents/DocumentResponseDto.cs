namespace ConstructionProjectTracker.API.DTOs.Documents;

public class DocumentResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}
