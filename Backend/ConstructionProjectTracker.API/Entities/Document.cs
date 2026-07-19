namespace ConstructionProjectTracker.API.Entities;

public class Document
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UploadedByUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = "Other";
    public string RelativeFilePath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
