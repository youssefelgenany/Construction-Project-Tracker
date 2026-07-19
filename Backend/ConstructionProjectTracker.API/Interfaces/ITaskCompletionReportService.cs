using ConstructionProjectTracker.API.DTOs.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskCompletionReportService
{
    Task<TaskCompletionReportDto> UploadAsync(int taskId, IFormFile file, int userId);

    Task<FileStreamResult?> DownloadAsync(int taskId, int userId, bool isAdmin);

    Task<TaskResponseDto> ApproveAsync(int taskId, int userId);

    Task<TaskResponseDto> RejectAsync(int taskId, RejectCompletionReportDto dto, int userId);

    Task DeletePhysicalFileAsync(string relativeFilePath);
}
