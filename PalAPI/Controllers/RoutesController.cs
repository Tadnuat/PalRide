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
            try
            {
                var userId = GetCurrentUserId();
                var result = await _routeService.RegisterRouteAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while registering route. Please try again later." });
            }
        }

        /// <summary>
        /// Cập nhật tuyến đường
        /// </summary>
        [HttpPut("{routeId}")]
        public async Task<IActionResult> UpdateRoute(int routeId, UpdateRouteDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _routeService.UpdateRouteAsync(userId, routeId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating route. Please try again later." });
            }
        }

        /// <summary>
        /// Xóa tuyến đường
        /// </summary>
        [HttpDelete("{routeId}")]
        public async Task<IActionResult> DeleteRoute(int routeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _routeService.DeleteRouteAsync(userId, routeId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while deleting route. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy danh sách tuyến đường của user
        /// </summary>
        [HttpGet("my-routes")]
        public async Task<IActionResult> GetMyRoutes()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _routeService.GetUserRoutesAsync(userId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your routes. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy thông tin tuyến đường theo ID
        /// </summary>
        [HttpGet("{routeId}")]
        public async Task<IActionResult> GetRouteById(int routeId)
        {
            try
            {
                var result = await _routeService.GetRouteByIdAsync(routeId);
                if (!result.IsSuccess) return NotFound(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving route details. Please try again later." });
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
    }
}


