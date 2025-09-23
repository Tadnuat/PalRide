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
            => Ok(await _adminAuthService.LoginAsync(dto));
    }
}





