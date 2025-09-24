using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService;
using PalService.Interface;
using System.Security.Claims;
using System.Text.Json;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDtos dto)
            => Ok(await _authService.LoginAsync(dto));

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
            => Ok(await _authService.RegisterAsync(dto));

        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
            => Ok(await _authService.LoginWithGoogleAsync(dto));


        [HttpGet("google-login-url")]
        public IActionResult GoogleLoginUrl([FromServices] IConfiguration config)
        {
            var clientId = config["GoogleAuthSettings:ClientId"];
            if (string.IsNullOrEmpty(clientId))
                return BadRequest(new { error = "Google Client ID is not configured" });

            // For mobile apps, return the client ID so they can configure Google Sign-In SDK
            return Ok(new { 
                clientId = clientId,
                message = "Use this clientId to configure Google Sign-In in your mobile app. Send the IdToken to /api/auth/google-login endpoint."
            });
        }


        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
            => Ok(await _authService.VerifyOtpAsync(dto));

        [Authorize]
        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, UpdateUserDto dto)
            => Ok(await _authService.UpdateUserAsync(userId, dto));

        [HttpDelete("delete/{email}")]
        public async Task<IActionResult> DeleteUser(string email)
            => Ok(await _authService.DeleteUserAsync(email));

        [Authorize]
        [HttpPut("{userId}/active")]
        public async Task<IActionResult> SetActive(int userId, [FromBody] SetActiveDto dto)
            => Ok(await _authService.SetUserActiveAsync(userId, dto.IsActive));

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
            => Ok(await _authService.ForgotPasswordAsync(dto));

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
            => Ok(await _authService.ResetPasswordAsync(dto));

            [HttpPost("change-password")]
            public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
                => Ok(await _authService.ChangePasswordAsync(dto));

            [HttpGet("profile")]
            [Authorize]
            public async Task<IActionResult> GetProfile()
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await _authService.GetProfileAsync(userId));
            }

            [HttpGet("test-roles")]
            [Authorize]
            public IActionResult TestRoles()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = User.FindFirstValue(ClaimTypes.Name);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                
                return Ok(new
                {
                    UserId = userId,
                    UserName = userName,
                    Role = userRole,
                    Message = $"User {userName} with role {userRole} can access this endpoint"
                });
            }
    }
}
