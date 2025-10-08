using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VouchersController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VouchersController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        /// <summary>
        /// Tạo voucher mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateVoucherDto dto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _voucherService.CreateVoucherAsync(dto, adminId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while creating voucher. Please try again later." });
            }
        }

        /// <summary>
        /// Cập nhật voucher (Admin only)
        /// </summary>
        [HttpPut("{voucherId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int voucherId, [FromBody] UpdateVoucherDto dto)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _voucherService.UpdateVoucherAsync(voucherId, dto, adminId);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating voucher. Please try again later." });
            }
        }

        /// <summary>
        /// Xóa voucher (Admin only)
        /// </summary>
        [HttpDelete("{voucherId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int voucherId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var result = await _voucherService.DeleteVoucherAsync(voucherId, adminId);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while deleting voucher. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy thông tin voucher theo ID
        /// </summary>
        [HttpGet("{voucherId}")]
        [Authorize]
        public async Task<IActionResult> GetById(int voucherId)
        {
            try
            {
                var result = await _voucherService.GetVoucherByIdAsync(voucherId);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving voucher details. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy tất cả vouchers (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _voucherService.GetAllVouchersAsync();
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving all vouchers. Please try again later." });
            }
        }

        /// <summary>
        /// Lấy danh sách vouchers đang hoạt động
        /// </summary>
        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActive()
        {
            try
            {
                var result = await _voucherService.GetActiveVouchersAsync();
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving active vouchers. Please try again later." });
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


