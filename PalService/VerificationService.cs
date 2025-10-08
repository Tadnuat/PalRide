using PalRepository.PalRepository;
using PalRepository.Models;
using PalService.Interface;
using System;
using System.Threading.Tasks;

namespace PalService
{
    public class VerificationService : IVerificationService
    {
        private readonly UserRepository _userRepo;

        public VerificationService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<bool> IsDriverVerifiedAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            // Driver cần cả bằng lái và căn cước đã verified
            return user.DriverLicenseVerified == true && user.CitizenIdVerified == true;
        }

        public async Task<bool> IsPassengerVerifiedAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            // Passenger chỉ cần căn cước đã verified
            return user.CitizenIdVerified == true;
        }

        public async Task<bool> IsUserVerifiedForRoleAsync(int userId, string role)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            return role.ToLower() switch
            {
                "driver" => user.DriverLicenseVerified == true && user.CitizenIdVerified == true,
                "passenger" => user.CitizenIdVerified == true,
                "both" => user.DriverLicenseVerified == true && user.CitizenIdVerified == true,
                _ => false
            };
        }

        public async Task<string> GetVerificationErrorMessageAsync(int userId, string role)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return "User not found";

            return role.ToLower() switch
            {
                "driver" => "Driver license and citizen ID must be verified to perform this action",
                "passenger" => "Citizen ID must be verified to perform this action",
                "both" => "Driver license and citizen ID must be verified to perform this action",
                _ => "Invalid role"
            };
        }
    }
}

