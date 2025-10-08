using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUserController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        /// <summary>
        /// Cập nhật trạng thái xác minh bằng lái xe
        /// </summary>
        [HttpPut("{userId}/driver-license-verification")]
        public async Task<IActionResult> UpdateDriverLicenseVerification(int userId, [FromBody] bool isVerified)
        {
            try
            {
                var result = await _adminUserService.UpdateDriverLicenseVerificationAsync(userId, isVerified);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating driver license verification. Please try again later." });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái xác minh căn cước công dân
        /// </summary>
        [HttpPut("{userId}/citizen-id-verification")]
        public async Task<IActionResult> UpdateCitizenIdVerification(int userId, [FromBody] bool isVerified)
        {
            try
            {
                var result = await _adminUserService.UpdateCitizenIdVerificationAsync(userId, isVerified);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating citizen ID verification. Please try again later." });
            }
        }

        /// <summary>
        /// Cập nhật thông tin tài liệu người dùng
        /// </summary>
        [HttpPut("{userId}/documents")]
        public async Task<IActionResult> UpdateUserDocuments(int userId, [FromBody] UpdateUserDocumentsDto dto)
        {
            try
            {
                var result = await _adminUserService.UpdateUserDocumentsAsync(userId, dto);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating user documents. Please try again later." });
            }
        }
    }
}

