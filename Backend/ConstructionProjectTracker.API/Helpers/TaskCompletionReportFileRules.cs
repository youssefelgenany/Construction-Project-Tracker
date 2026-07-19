namespace ConstructionProjectTracker.API.Helpers;

public static class TaskCompletionReportFileRules
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".dwg", ".zip"
    };

    public const string UploadRootFolder = "uploads/tasks";

    public static bool IsAllowedExtension(string extension) =>
        AllowedExtensions.Contains(extension.ToLowerInvariant());

    public static string GetRelativeDirectory(int taskId) =>
        $"{UploadRootFolder}/{taskId}";

    public static string BuildStoredFileName(string extension) =>
        $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
}
