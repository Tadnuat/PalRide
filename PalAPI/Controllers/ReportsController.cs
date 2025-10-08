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
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Tạo báo cáo mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reportService.CreateReportAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while creating report. Please try again later." });
            }
        }

        /// <summary>
        /// Cập nhật báo cáo (Admin only)
        /// </summary>
        [HttpPut("{reportId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReport(int reportId, [FromBody] UpdateReportDto dto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _reportService.UpdateReportAsync(reportId, adminId, dto);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating report. Please try again later." });
            }
        }

        /// <summary>
        /// Xóa báo cáo (Admin only)
        /// </summary>
        [HttpDelete("{reportId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _reportService.DeleteReportAsync(reportId, adminId);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while deleting report. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy thông tin báo cáo theo ID
        /// </summary>
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            try
            {
                var result = await _reportService.GetReportByIdAsync(reportId);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving report details. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy tất cả báo cáo với filter (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllReports([FromQuery] ReportFilterDto filter)
        {
            try
            {
                var result = await _reportService.GetAllReportsAsync(filter);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving all reports. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy báo cáo của user hiện tại
        /// </summary>
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reportService.GetUserReportsAsync(userId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your reports. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy thống kê báo cáo (Admin only)
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportStats()
        {
            try
            {
                var result = await _reportService.GetReportStatsAsync();
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving report statistics. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy báo cáo theo trạng thái (Admin only)
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportsByStatus(string status)
        {
            try
            {
                var result = await _reportService.GetReportsByStatusAsync(status);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving reports by status. Please try again later." });
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



