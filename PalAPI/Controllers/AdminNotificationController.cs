using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            var result = await _notificationService.CreateNotificationAsync(dto);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPost("create-bulk")]
        public async Task<IActionResult> CreateBulkNotification([FromBody] CreateBulkNotificationDto dto)
        {
            var result = await _notificationService.CreateBulkNotificationAsync(dto);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            var result = await _notificationService.GetUserNotificationsAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }
    }
}

