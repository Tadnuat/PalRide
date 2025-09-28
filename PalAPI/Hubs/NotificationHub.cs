using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PalAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task JoinUserGroup()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
        }

        public async Task JoinRoleGroup(string role)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
        }

        public async Task JoinTripGroup(int tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"trip_{tripId}");
        }

        public async Task LeaveUserGroup()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
        }

        public async Task LeaveRoleGroup(string role)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{role}");
        }

        public async Task LeaveTripGroup(int tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip_{tripId}");
        }

        // Chat methods
        public async Task JoinChatGroup(int tripId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{tripId}");
            }
        }

        public async Task LeaveChatGroup(int tripId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{tripId}");
        }

        public async Task SendTyping(int toUserId, int tripId, bool isTyping)
        {
            var fromUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(fromUserId))
            {
                await Clients.Group($"user_{toUserId}").SendAsync("ReceiveTyping", new
                {
                    FromUserId = int.Parse(fromUserId),
                    ToUserId = toUserId,
                    TripId = tripId,
                    IsTyping = isTyping,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{userRole}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{userRole}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}


