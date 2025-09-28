using PalService.DTOs;

namespace PalService.Interface
{
    public interface INotificationService
    {
        Task<ResponseDto<NotificationDto>> CreateNotificationAsync(CreateNotificationDto dto);
        Task<ResponseDto<int>> CreateBulkNotificationAsync(CreateBulkNotificationDto dto);
        Task<ResponseDto<List<NotificationDto>>> GetUserNotificationsAsync(int userId);
        Task<ResponseDto<bool>> MarkAsReadAsync(int notificationId, int userId);
        Task<ResponseDto<bool>> MarkAllAsReadAsync(int userId);
        Task<ResponseDto<int>> GetUnreadCountAsync(int userId);
        
        // Helper methods for automatic notifications
        Task SendAndSaveNotificationToUserAsync(int userId, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null);
        Task SendAndSaveNotificationToRoleAsync(string role, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null);
        Task SendAndSaveNotificationToTripAsync(int tripId, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null);
    }
}
