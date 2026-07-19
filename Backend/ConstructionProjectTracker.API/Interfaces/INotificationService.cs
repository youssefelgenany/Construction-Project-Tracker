using ConstructionProjectTracker.API.DTOs.Notifications;
using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Interfaces;

public interface INotificationService
{
    Task NotifyUsersAsync(
        IEnumerable<int> userIds,
        NotificationType type,
        string title,
        string message,
        string? relatedEntityType = null,
        int? relatedEntityId = null);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, int take = 50);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
