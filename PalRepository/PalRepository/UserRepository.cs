using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalRepository.PalRepository
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(PalRideContext context) : base(context) { }

        public User? GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public User? GetByPhoneNumber(string phoneNumber)
        {
            return _context.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<bool> UpdateDriverLicenseVerificationAsync(int userId, bool isVerified)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.DriverLicenseVerified = isVerified;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCitizenIdVerificationAsync(int userId, bool isVerified)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.CitizenIdVerified = isVerified;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserDocumentsAsync(int userId, string? driverLicenseNumber, string? driverLicenseImage, string? citizenId, string? citizenIdImage)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!string.IsNullOrEmpty(driverLicenseNumber))
                user.DriverLicenseNumber = driverLicenseNumber;
            if (!string.IsNullOrEmpty(driverLicenseImage))
                user.DriverLicenseImage = driverLicenseImage;
            if (!string.IsNullOrEmpty(citizenId))
                user.CitizenId = citizenId;
            if (!string.IsNullOrEmpty(citizenIdImage))
                user.CitizenIdImage = citizenIdImage;

            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
