using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ISignalRNotificationService _signalRService;
        private readonly PalRideContext _context;

        public ChatController(IChatService chatService, ISignalRNotificationService signalRService, PalRideContext context)
        {
            _chatService = chatService;
            _signalRService = signalRService;
            _context = context;
        }

        /// <summary>
        /// Gửi tin nhắn
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto sendMessageDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _chatService.SendMessageAsync(userId, sendMessageDto);
                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử chat của một chuyến đi
        /// </summary>
        [HttpGet("history/{tripId}")]
        public async Task<ActionResult<ChatHistoryDto>> GetChatHistory(int tripId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var messages = await _chatService.GetChatHistoryAsync(userId, tripId);
                
                // Lấy thông tin trip và người chat
                var trip = await GetTripInfo(tripId);
                var otherUser = await GetOtherUserInfo(userId, tripId);
                
                var chatHistory = new ChatHistoryDto
                {
                    TripId = tripId,
                    TripInfo = $"{trip.PickupLocation} → {trip.DropoffLocation}. {trip.StartTime:dd/MM HH:mm}",
                    Messages = messages,
                    OtherUserId = otherUser.UserId,
                    OtherUserName = otherUser.FullName,
                    OtherUserRole = otherUser.Role
                };

                return Ok(chatHistory);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách các cuộc trò chuyện
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<List<ChatListDto>>> GetChatList()
        {
            try
            {
                var userId = GetCurrentUserId();
                var chatList = await _chatService.GetChatListAsync(userId);
                return Ok(chatList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu tin nhắn đã đọc
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<ActionResult> MarkAsRead([FromBody] MarkAsReadDto markAsReadDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatService.MarkMessagesAsReadAsync(userId, markAsReadDto);
                
                // Gửi thông báo đã đọc qua SignalR
                await _signalRService.SendMessageReadAsync(markAsReadDto.FromUserId, userId, markAsReadDto.TripId);
                
                return Ok(new { message = "Messages marked as read" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy số lượng tin nhắn chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _chatService.GetUnreadMessageCountAsync(userId);
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi thông báo đang gõ
        /// </summary>
        [HttpPost("typing")]
        public async Task<ActionResult> SendTyping([FromBody] TypingDto typingDto)
        {
            try
            {
                var fromUserId = GetCurrentUserId();
                await _signalRService.SendChatTypingAsync(fromUserId, typingDto.ToUserId, typingDto.TripId, typingDto.IsTyping);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }
            return userId;
        }

        private async Task<dynamic> GetTripInfo(int tripId)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.TripId == tripId);
            
            if (trip == null)
            {
                throw new KeyNotFoundException("Trip not found");
            }

            return new
            {
                PickupLocation = trip.PickupLocation,
                DropoffLocation = trip.DropoffLocation,
                StartTime = trip.StartTime
            };
        }

        private async Task<dynamic> GetOtherUserInfo(int userId, int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Bookings)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                throw new KeyNotFoundException("Trip not found");
            }

            // Xác định người chat với user hiện tại
            if (trip.DriverId == userId)
            {
                // User là driver, lấy passenger đầu tiên
                var firstBooking = trip.Bookings.FirstOrDefault();
                if (firstBooking != null)
                {
                    var passenger = await _context.Users.FindAsync(firstBooking.PassengerId);
                    return new
                    {
                        UserId = passenger?.UserId ?? 0,
                        FullName = passenger?.FullName ?? "Unknown",
                        Role = "Passenger"
                    };
                }
            }
            else
            {
                // User là passenger, lấy driver
                return new
                {
                    UserId = trip.Driver.UserId,
                    FullName = trip.Driver.FullName,
                    Role = "Driver"
                };
            }

            throw new KeyNotFoundException("Other user not found");
        }
    }

    public class TypingDto
    {
        public int ToUserId { get; set; }
        public int TripId { get; set; }
        public bool IsTyping { get; set; }
    }
}
