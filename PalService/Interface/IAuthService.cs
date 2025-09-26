using PalService.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDtos dto);
        Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto);
        Task<RegisterResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto);
        Task<ResponseDto<bool>> ResendOtpAsync(ResendOtpDto dto);
        Task<ResponseDto<UserDto>> UpdateUserAsync(int userId, UpdateUserDto dto);
        Task<ResponseDto<bool>> DeleteUserAsync(string email);
        Task<ResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ResponseDto<bool>> ResetPasswordAsync(ResetPasswordDto dto);
        Task<ResponseDto<bool>> ChangePasswordAsync(ChangePasswordDto dto);
        Task<ResponseDto<UserDto>> GetProfileAsync(int userId);
        Task<ResponseDto<bool>> SetUserActiveAsync(int userId, bool isActive);
    }
}
