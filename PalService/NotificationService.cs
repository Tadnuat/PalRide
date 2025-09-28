using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.DTOs;
using PalService.Interface;

namespace PalService
{
    public class NotificationService : INotificationService
    {
        private readonly PalRideContext _context;
        private readonly ISignalRNotificationService _signalRService;
        private readonly GenericRepository<Notification> _notificationRepo;
        private readonly UserRepository _userRepo;

        public NotificationService(
            PalRideContext context, 
            ISignalRNotificationService signalRService,
            GenericRepository<Notification> notificationRepo,
            UserRepository userRepo)
        {
            _context = context;
            _signalRService = signalRService;
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
        }

        public async Task<ResponseDto<NotificationDto>> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var response = new ResponseDto<NotificationDto>();
            try
            {
                var notification = new Notification
                {
                    UserId = dto.TargetUserId,
                    UserRole = dto.TargetRole,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId
                };

                await _notificationRepo.CreateAsync(notification);

                // Send SignalR notification
                var signalRNotification = new SignalRNotificationDto
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    Timestamp = DateTime.UtcNow,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId
                };

                if (dto.TargetUserId.HasValue)
                {
                    await _signalRService.SendNotificationToUserAsync(dto.TargetUserId.Value, signalRNotification);
                }
                else if (!string.IsNullOrEmpty(dto.TargetRole))
                {
                    await _signalRService.SendNotificationToRoleAsync(dto.TargetRole, signalRNotification);
                }
                else
                {
                    await _signalRService.SendNotificationToAllAsync(signalRNotification);
                }

                response.Result = new NotificationDto
                {
                    NotificationId = notification.NotificationId,
                    UserId = notification.UserId,
                    UserRole = notification.UserRole,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                    RelatedEntityType = notification.RelatedEntityType,
                    RelatedEntityId = notification.RelatedEntityId
                };
                response.Message = "Notification created and sent successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<int>> CreateBulkNotificationAsync(CreateBulkNotificationDto dto)
        {
            var response = new ResponseDto<int>();
            try
            {
                if (dto.TargetUserIds == null || !dto.TargetUserIds.Any())
                {
                    response.IsSuccess = false;
                    response.Message = "TargetUserIds is required";
                    return response;
                }

                var notifications = new List<Notification>();
                var signalRNotification = new SignalRNotificationDto
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    Timestamp = DateTime.UtcNow,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId
                };

                foreach (var userId in dto.TargetUserIds)
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = dto.Title,
                        Message = dto.Message,
                        Type = dto.Type,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        RelatedEntityType = dto.RelatedEntityType,
                        RelatedEntityId = dto.RelatedEntityId
                    };
                    notifications.Add(notification);

                    // Send SignalR notification to each user
                    await _signalRService.SendNotificationToUserAsync(userId, signalRNotification);
                }

                await _context.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                response.Result = notifications.Count;
                response.Message = $"Bulk notification sent to {notifications.Count} users successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<NotificationDto>>> GetUserNotificationsAsync(int userId)
        {
            var response = new ResponseDto<List<NotificationDto>>();
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId || n.UserId == null)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                response.Result = notifications.Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    UserRole = n.UserRole,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RelatedEntityType = n.RelatedEntityType,
                    RelatedEntityId = n.RelatedEntityId
                }).ToList();
                response.Message = "Notifications retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> MarkAsReadAsync(int notificationId, int userId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var notification = await _notificationRepo.GetByIdAsync(notificationId);
                if (notification == null || (notification.UserId != null && notification.UserId != userId))
                {
                    response.IsSuccess = false;
                    response.Message = "Notification not found or access denied";
                    return response;
                }

                notification.IsRead = true;
                await _notificationRepo.UpdateAsync(notification);

                response.Result = true;
                response.Message = "Notification marked as read";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> MarkAllAsReadAsync(int userId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

                response.Result = true;
                response.Message = "All notifications marked as read";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<int>> GetUnreadCountAsync(int userId)
        {
            var response = new ResponseDto<int>();
            try
            {
                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                response.Result = count;
                response.Message = "Unread count retrieved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        // Helper methods for automatic notifications
        public async Task SendAndSaveNotificationToUserAsync(int userId, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            try
            {
                // Create and save notification to database
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                await _notificationRepo.CreateAsync(notification);
                await _context.SaveChangesAsync();

                // Send SignalR notification
                var signalRNotification = new SignalRNotificationDto
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                await _signalRService.SendNotificationToUserAsync(userId, signalRNotification);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main flow
                Console.WriteLine($"Error sending notification to user {userId}: {ex.Message}");
            }
        }

        public async Task SendAndSaveNotificationToRoleAsync(string role, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            try
            {
                // Get all users with the specified role
                var users = await _context.Users
                    .Where(u => u.Role == role || (role == "Both" && (u.Role == "Driver" || u.Role == "Passenger")))
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (!users.Any()) return;

                // Create notifications for all users
                var notifications = users.Select(userId => new Notification
                {
                    UserId = userId,
                    UserRole = role,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                }).ToList();

                await _context.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                // Send SignalR notification to role
                var signalRNotification = new SignalRNotificationDto
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                await _signalRService.SendNotificationToRoleAsync(role, signalRNotification);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main flow
                Console.WriteLine($"Error sending notification to role {role}: {ex.Message}");
            }
        }

        public async Task SendAndSaveNotificationToTripAsync(int tripId, string title, string message, string type, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            try
            {
                // Get all passengers in the trip
                var passengerIds = await _context.Bookings
                    .Where(b => b.TripId == tripId && b.Status != "Cancelled")
                    .Select(b => b.PassengerId)
                    .ToListAsync();

                if (!passengerIds.Any()) return;

                // Create notifications for all passengers
                var notifications = passengerIds.Select(passengerId => new Notification
                {
                    UserId = passengerId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                }).ToList();

                await _context.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                // Send SignalR notification to trip
                var signalRNotification = new SignalRNotificationDto
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                await _signalRService.SendNotificationToTripAsync(tripId, signalRNotification);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main flow
                Console.WriteLine($"Error sending notification to trip {tripId}: {ex.Message}");
            }
        }

    }
}
