using Microsoft.AspNetCore.SignalR;
using PalAPI.Hubs;
using PalService.DTOs;
using PalService.Interface;

namespace PalAPI.Services
{
    public class SignalRNotificationService : ISignalRNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(int userId, SignalRNotificationDto notification)
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task SendNotificationToRoleAsync(string role, SignalRNotificationDto notification)
        {
            await _hubContext.Clients.Group($"role_{role}").SendAsync("ReceiveNotification", notification);
        }

        public async Task SendNotificationToTripAsync(int tripId, SignalRNotificationDto notification)
        {
            await _hubContext.Clients.Group($"trip_{tripId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task SendNotificationToAllAsync(SignalRNotificationDto notification)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }

        public async Task SendChatMessageAsync(MessageDto message)
        {
            // Gửi tin nhắn đến người nhận
            await _hubContext.Clients.Group($"user_{message.ToUserId}").SendAsync("ReceiveChatMessage", message);
            
            // Gửi tin nhắn đến người gửi để confirm
            await _hubContext.Clients.Group($"user_{message.FromUserId}").SendAsync("ChatMessageSent", message);
        }

        public async Task SendChatTypingAsync(int fromUserId, int toUserId, int tripId, bool isTyping)
        {
            var typingData = new
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                TripId = tripId,
                IsTyping = isTyping,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{toUserId}").SendAsync("ReceiveTyping", typingData);
        }

        public async Task SendMessageReadAsync(int fromUserId, int toUserId, int tripId)
        {
            var readData = new
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                TripId = tripId,
                ReadAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"user_{fromUserId}").SendAsync("MessageRead", readData);
        }
    }
}


