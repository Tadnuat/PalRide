using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAdminAuthService _adminAuthService;
        public AdminAuthController(IAdminAuthService adminAuthService) => _adminAuthService = adminAuthService;

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDtos dto)
        {
            try
            {
                var result = await _adminAuthService.LoginAsync(dto);
                return Ok(result);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred during admin login. Please try again later." });
            }
        }
    }
}

















