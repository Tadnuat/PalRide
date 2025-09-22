namespace PalService.DTOs
{
    public class ResponseDto<T>
    {
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; }
        public T Result { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    public class RegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } = "Both";
    }

    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
    }

    public class ChangePasswordDto
    {
        public string Email { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}


