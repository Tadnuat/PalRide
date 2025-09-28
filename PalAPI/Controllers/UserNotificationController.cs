using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class UserNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public UserNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _notificationService.GetUserNotificationsAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _notificationService.MarkAsReadAsync(notificationId, userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _notificationService.MarkAllAsReadAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _notificationService.GetUnreadCountAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }
    }
}


