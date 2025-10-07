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
        public string Introduce { get; set; }
        public string University { get; set; }
        public string StudentId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string? Avatar { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseImage { get; set; }
        public string? CitizenId { get; set; }
        public string? CitizenIdImage { get; set; }
        public bool? DriverLicenseVerified { get; set; }
        public bool? CitizenIdVerified { get; set; }
    }

    public class LoginDtos
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

    public class RegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public bool RequiresVerification { get; set; }
    }

    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class ResendOtpDto
    {
        public string Email { get; set; }
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; }
        public string? Introduce { get; set; }
        public string? University { get; set; }
        public string? StudentId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Avatar { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseImage { get; set; }
        public string? CitizenId { get; set; }
        public string? CitizenIdImage { get; set; }
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

    public class SetActiveDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateUserDocumentsDto
    {
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseImage { get; set; }
        public string? CitizenId { get; set; }
        public string? CitizenIdImage { get; set; }
    }
}



