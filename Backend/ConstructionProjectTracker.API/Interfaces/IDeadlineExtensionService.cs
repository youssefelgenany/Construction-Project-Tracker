using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IDeadlineExtensionService
{
    Task<DeadlineExtensionRequestDto> CreateTaskRequestAsync(
        int taskId,
        CreateTaskDeadlineExtensionRequestDto dto,
        int userId);

    Task<DeadlineExtensionRequestDto> CreateProjectRequestAsync(
        int projectId,
        CreateProjectDeadlineExtensionRequestDto dto,
        int userId);

    Task<DeadlineExtensionRequestDto?> GetLatestTaskRequestAsync(int taskId, int userId, bool isAdmin);
    Task<DeadlineExtensionRequestDto?> GetLatestProjectRequestAsync(int projectId, int userId, bool isAdmin);

    Task<IReadOnlyList<DeadlineExtensionRequestDto>> GetAdminRequestsAsync(ExtensionRequestStatus? status);

    Task<DeadlineExtensionRequestDto> ApproveTaskRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId);

    Task<DeadlineExtensionRequestDto> RejectTaskRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId);

    Task<DeadlineExtensionRequestDto> ApproveProjectRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId);

    Task<DeadlineExtensionRequestDto> RejectProjectRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId);

    Task ExtendTaskDeadlineAsync(int taskId, AdminExtendTaskDeadlineDto dto, int adminUserId);
    Task ExtendProjectDeadlineAsync(int projectId, AdminExtendProjectDeadlineDto dto, int adminUserId);

    Task<IReadOnlyList<TaskDeadlineHistoryDto>> GetTaskHistoryAsync(int taskId, int userId, bool isAdmin);
    Task<IReadOnlyList<ProjectDeadlineHistoryDto>> GetProjectHistoryAsync(int projectId, int userId, bool isAdmin);
}
