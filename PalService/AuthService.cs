using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PalService.DTOs;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.Interface;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace PalService
{
    public class AuthService : IAuthService
    {
        private readonly UserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly PasswordResetTokenRepository _tokenRepo;

        public AuthService(UserRepository userRepo, IConfiguration config, IEmailService emailService, PasswordResetTokenRepository tokenRepo)
        {
            _userRepo = userRepo;
            _config = config;
            _emailService = emailService;
            _tokenRepo = tokenRepo;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDtos dto)
        {
            var user = _userRepo.GetByEmail(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto)
        {
            // Xác thực IdToken của Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["GoogleAuthSettings:ClientId"] } // chỉ chấp nhận token của frontend
                });
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid Google token: " + ex.Message);
            }

            // payload.Email, payload.Name đã được xác thực
            var user = await _userRepo.GetByEmailAsync(payload.Email);

            if (user == null)
            {
                // Tạo user mới
                user = new User
                {
                    FullName = payload.Name,
                    Email = payload.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("string123"),
                    Role = "Both",
                    PhoneNumber = "",
                    GmailVerified = true,
                    IsActive = true
                };
                await _userRepo.CreateAsync(user);
            }
            else
            {
                // Cập nhật tên nếu khác
                if (user.FullName != payload.Name)
                {
                    user.FullName = payload.Name;
                    await _userRepo.UpdateAsync(user);
                }
            }

            return await GenerateTokensAsync(user);
        }


        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
                throw new InvalidOperationException("FullName, Email and Password are required");

            var existing = _userRepo.GetByEmail(dto.Email);
            if (existing != null)
                throw new InvalidOperationException("Email already registered");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Both" : dto.Role,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                GmailVerified = false,
                IsActive = true
            };

            await _userRepo.CreateAsync(user);

            // Generate OTP (6 digits) and email it
            var otp = new Random().Next(100000, 999999).ToString();
            await _tokenRepo.CreateAsync(new PasswordResetToken
            {
                UserId = user.UserId,
                Token = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Used = false,
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await _emailService.SendEmailAsync(user.Email, "Your PalRide OTP",
                    $"<p>Hi {user.FullName},</p><p>Your verification code is:</p><h2>{otp}</h2><p>This code expires in 10 minutes.</p>");
            }
            catch
            {
                // Swallow email errors so registration still succeeds; OTP exists in DB
            }
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);
            if (user == null) throw new KeyNotFoundException("User not found");

            var token = await _tokenRepo.GetActiveByUserAndTokenAsync(user.UserId, dto.Otp);
            if (token == null) throw new UnauthorizedAccessException("Invalid or expired OTP");

            await _tokenRepo.MarkUsedAsync(token);
            user.GmailVerified = true;
            await _userRepo.UpdateAsync(user);

            return await GenerateTokensAsync(user);
        }

        public async Task<ResponseDto<UserDto>> UpdateUserAsync(int userId, UpdateUserDto dto)
        {
            var response = new ResponseDto<UserDto>();
            try
            {
                var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");

                if (!user.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _userRepo.GetByEmailAsync(dto.Email);
                    if (exists != null) throw new InvalidOperationException("Email is already taken");
                }

                user.FullName = dto.FullName;
                user.Email = dto.Email;
                user.PhoneNumber = dto.PhoneNumber;
                user.Role = dto.Role;
                user.IsActive = dto.IsActive;
                user.Introduce = dto.Introduce;
                user.University = dto.University;
                user.StudentId = dto.StudentId;
                user.DateOfBirth = dto.DateOfBirth.HasValue ? DateOnly.FromDateTime(dto.DateOfBirth.Value.Date) : null;
                user.Gender = dto.Gender;
                await _userRepo.UpdateAsync(user);

                response.Result = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    Introduce = user.Introduce,
                    University = user.University,
                    StudentId = user.StudentId,
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = user.Gender
                };
                response.Message = "User updated successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> DeleteUserAsync(string email)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var user = await _userRepo.GetByEmailAsync(email) ?? throw new KeyNotFoundException("User not found");
                var success = await _userRepo.RemoveAsync(user);
                response.Result = success;
                response.Message = success ? "User deleted successfully" : "Failed to delete user";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var user = await _userRepo.GetByEmailAsync(dto.Email) ?? throw new KeyNotFoundException("User not found");
                var otp = new Random().Next(100000, 999999).ToString();
                await _tokenRepo.CreateAsync(new PasswordResetToken
                {
                    UserId = user.UserId,
                    Token = otp,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    Used = false,
                    CreatedAt = DateTime.UtcNow
                });
                try
                {
                    await _emailService.SendEmailAsync(user.Email, "Your password reset OTP", $"<p>OTP: <b>{otp}</b></p>");
                }
                catch
                {
                    // Ignore email errors; client can retry or use another channel
                }
                response.Result = true;
                response.Message = "OTP sent to your email";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var user = await _userRepo.GetByEmailAsync(dto.Email) ?? throw new KeyNotFoundException("User not found");
                var token = await _tokenRepo.GetActiveByUserAndTokenAsync(user.UserId, dto.Otp) ?? throw new UnauthorizedAccessException("Invalid or expired OTP");
                await _tokenRepo.MarkUsedAsync(token);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _userRepo.UpdateAsync(user);
                response.Result = true;
                response.Message = "Password reset successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var user = await _userRepo.GetByEmailAsync(dto.Email) ?? throw new KeyNotFoundException("User not found");
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                    throw new UnauthorizedAccessException("Current password is incorrect");
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _userRepo.UpdateAsync(user);
                response.Result = true;
                response.Message = "Password changed successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<UserDto>> GetProfileAsync(int userId)
        {
            var response = new ResponseDto<UserDto>();
            try
            {
                var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
                response.Result = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    Introduce = user.Introduce,
                    University = user.University,
                    StudentId = user.StudentId,
                    DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    Gender = user.Gender
                };
                response.Message = "Profile retrieved";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }



        private async Task<AuthResponseDto> GenerateTokensAsync(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: creds
            );

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.Now.AddDays(7);

            await _userRepo.UpdateAsync(user);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        }
    }
}
