using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalService
{
    public class ChatService : IChatService
    {
        private readonly PalRideContext _context;
        private readonly ISignalRNotificationService _signalRService;

        public ChatService(PalRideContext context, ISignalRNotificationService signalRService)
        {
            _context = context;
            _signalRService = signalRService;
        }

        public async Task<MessageDto> SendMessageAsync(int fromUserId, SendMessageDto sendMessageDto)
        {
            // Kiểm tra quyền chat
            if (!await CanUserChatAsync(fromUserId, sendMessageDto.TripId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chat trong chuyến đi này");
            }

            // Kiểm tra trip có tồn tại không
            var trip = await _context.Trips
                .Include(t => t.Driver)
                .FirstOrDefaultAsync(t => t.TripId == sendMessageDto.TripId);
            
            if (trip == null)
            {
                throw new KeyNotFoundException("Chuyến đi không tồn tại");
            }

            // Kiểm tra người nhận có tồn tại không
            var toUser = await _context.Users.FindAsync(sendMessageDto.ToUserId);
            if (toUser == null)
            {
                throw new KeyNotFoundException("Người nhận không tồn tại");
            }

            // Tạo message mới
            var message = new Message
            {
                TripId = sendMessageDto.TripId,
                FromUserId = fromUserId,
                ToUserId = sendMessageDto.ToUserId,
                MessageText = sendMessageDto.MessageText,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Load thông tin người gửi và người nhận
            var fromUser = await _context.Users.FindAsync(fromUserId);

            // Tạo DTO response
            var messageDto = new MessageDto
            {
                MessageId = message.MessageId,
                TripId = message.TripId ?? 0,
                FromUserId = message.FromUserId,
                ToUserId = message.ToUserId,
                MessageText = message.MessageText,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                FromUserName = fromUser?.FullName ?? "Unknown",
                ToUserName = toUser.FullName
            };

            // Gửi thông báo realtime qua SignalR
            await _signalRService.SendChatMessageAsync(messageDto);

            return messageDto;
        }

        public async Task<List<MessageDto>> GetChatHistoryAsync(int userId, int tripId)
        {
            // Kiểm tra quyền xem chat
            if (!await CanUserChatAsync(userId, tripId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem chat của chuyến đi này");
            }

            var messages = await _context.Messages
                .Include(m => m.FromUser)
                .Include(m => m.ToUser)
                .Where(m => m.TripId == tripId && 
                           (m.FromUserId == userId || m.ToUserId == userId))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    TripId = m.TripId ?? 0,
                    FromUserId = m.FromUserId,
                    ToUserId = m.ToUserId,
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    FromUserName = m.FromUser.FullName,
                    ToUserName = m.ToUser.FullName
                })
                .ToListAsync();

            return messages;
        }

        public async Task<List<ChatListDto>> GetChatListAsync(int userId)
        {
            // Lấy danh sách các trip mà user đã tham gia (driver hoặc passenger)
            var userTrips = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Bookings)
                .Where(t => t.DriverId == userId || t.Bookings.Any(b => b.PassengerId == userId))
                .ToListAsync();

            var chatList = new List<ChatListDto>();

            foreach (var trip in userTrips)
            {
                // Xác định người chat với user hiện tại
                User? otherUser = null;
                string otherUserRole = "";

                if (trip.DriverId == userId)
                {
                    // User là driver, lấy passenger đầu tiên
                    var firstBooking = trip.Bookings.FirstOrDefault();
                    if (firstBooking != null)
                    {
                        otherUser = await _context.Users.FindAsync(firstBooking.PassengerId);
                        otherUserRole = "Passenger";
                    }
                }
                else
                {
                    // User là passenger, lấy driver
                    otherUser = trip.Driver;
                    otherUserRole = "Driver";
                }

                if (otherUser == null) continue;

                // Lấy tin nhắn cuối cùng
                var lastMessage = await _context.Messages
                    .Where(m => m.TripId == trip.TripId && 
                               (m.FromUserId == userId || m.ToUserId == userId))
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                // Đếm tin nhắn chưa đọc
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.TripId == trip.TripId && 
                                   m.ToUserId == userId && 
                                   !m.IsRead);

                // Tạo thông tin trip
                var tripInfo = $"{trip.PickupLocation} → {trip.DropoffLocation}. {trip.StartTime:dd/MM HH:mm}";

                chatList.Add(new ChatListDto
                {
                    TripId = trip.TripId,
                    TripInfo = tripInfo,
                    OtherUserId = otherUser.UserId,
                    OtherUserName = otherUser.FullName,
                    OtherUserRole = otherUserRole,
                    LastMessage = lastMessage?.MessageText ?? "",
                    LastMessageTime = lastMessage?.CreatedAt,
                    HasUnreadMessages = unreadCount > 0,
                    UnreadCount = unreadCount
                });
            }

            return chatList.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        public async Task MarkMessagesAsReadAsync(int userId, MarkAsReadDto markAsReadDto)
        {
            var messages = await _context.Messages
                .Where(m => m.TripId == markAsReadDto.TripId && 
                           m.FromUserId == markAsReadDto.FromUserId && 
                           m.ToUserId == userId && 
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ToUserId == userId && !m.IsRead);
        }

        public async Task<bool> CanUserChatAsync(int userId, int tripId)
        {
            // Kiểm tra user có phải driver của trip không
            var isDriver = await _context.Trips
                .AnyAsync(t => t.TripId == tripId && t.DriverId == userId);

            if (isDriver) return true;

            // Kiểm tra user có phải passenger của trip không
            var isPassenger = await _context.Bookings
                .AnyAsync(b => b.TripId == tripId && b.PassengerId == userId);

            return isPassenger;
        }
    }
}
