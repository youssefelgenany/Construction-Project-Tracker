using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Engineer? Engineer { get; set; }
    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    public ICollection<TaskCompletionReport> UploadedCompletionReports { get; set; } = new List<TaskCompletionReport>();
    public ICollection<TaskCompletionReport> ReviewedCompletionReports { get; set; } = new List<TaskCompletionReport>();
}
