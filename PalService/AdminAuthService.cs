using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalService.DTOs;
using PalService.Interface;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PalService
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly AdminRepository _adminRepo;
        private readonly IConfiguration _config;

        public AdminAuthService(AdminRepository adminRepo, IConfiguration config)
        {
            _adminRepo = adminRepo;
            _config = config;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDtos dto)
        {
            var admin = await _adminRepo.GetByEmailAsync(dto.Email) ?? throw new UnauthorizedAccessException("Invalid credentials");
            if (!admin.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.FullName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = string.Empty,
                FullName = admin.FullName,
                Role = "Admin"
            };
        }
    }
}

















