using PalService.DTOs;

namespace PalService.Interface
{
    public interface ISignalRNotificationService
    {
        Task SendNotificationToUserAsync(int userId, SignalRNotificationDto notification);
        Task SendNotificationToRoleAsync(string role, SignalRNotificationDto notification);
        Task SendNotificationToTripAsync(int tripId, SignalRNotificationDto notification);
        Task SendNotificationToAllAsync(SignalRNotificationDto notification);
        
        // Chat methods
        Task SendChatMessageAsync(MessageDto message);
        Task SendChatTypingAsync(int fromUserId, int toUserId, int tripId, bool isTyping);
        Task SendMessageReadAsync(int fromUserId, int toUserId, int tripId);
    }
}


