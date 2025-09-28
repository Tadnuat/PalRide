using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RoutesController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        /// <summary>
        /// Đăng ký tuyến đường mới
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterRoute(CreateRouteDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _routeService.RegisterRouteAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật tuyến đường
        /// </summary>
        [HttpPut("{routeId}")]
        public async Task<IActionResult> UpdateRoute(int routeId, UpdateRouteDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _routeService.UpdateRouteAsync(userId, routeId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa tuyến đường
        /// </summary>
        [HttpDelete("{routeId}")]
        public async Task<IActionResult> DeleteRoute(int routeId)
        {
            var userId = GetCurrentUserId();
            var result = await _routeService.DeleteRouteAsync(userId, routeId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách tuyến đường của user
        /// </summary>
        [HttpGet("my-routes")]
        public async Task<IActionResult> GetMyRoutes()
        {
            var userId = GetCurrentUserId();
            var result = await _routeService.GetUserRoutesAsync(userId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin tuyến đường theo ID
        /// </summary>
        [HttpGet("{routeId}")]
        public async Task<IActionResult> GetRouteById(int routeId)
        {
            var result = await _routeService.GetRouteByIdAsync(routeId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
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
    }
}


