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
            var adminId = GetCurrentUserId();
            var result = await _voucherService.CreateVoucherAsync(dto, adminId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật voucher (Admin only)
        /// </summary>
        [HttpPut("{voucherId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int voucherId, [FromBody] UpdateVoucherDto dto)
        {
            var adminId = GetCurrentUserId();
            var result = await _voucherService.UpdateVoucherAsync(voucherId, dto, adminId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa voucher (Admin only)
        /// </summary>
        [HttpDelete("{voucherId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int voucherId)
        {
            var adminId = GetCurrentUserId();
            var result = await _voucherService.DeleteVoucherAsync(voucherId, adminId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin voucher theo ID
        /// </summary>
        [HttpGet("{voucherId}")]
        [Authorize]
        public async Task<IActionResult> GetById(int voucherId)
        {
            var result = await _voucherService.GetVoucherByIdAsync(voucherId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả vouchers (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _voucherService.GetAllVouchersAsync();
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách vouchers đang hoạt động
        /// </summary>
        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActive()
        {
            var result = await _voucherService.GetActiveVouchersAsync();
            if (!result.IsSuccess) return BadRequest(result);
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


