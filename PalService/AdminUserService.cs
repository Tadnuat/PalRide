using PalRepository.PalRepository;
using PalService.DTOs;
using PalService.Interface;
using System;
using System.Threading.Tasks;

namespace PalService
{
    public class AdminUserService : IAdminUserService
    {
        private readonly UserRepository _userRepo;

        public AdminUserService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<ResponseDto<bool>> UpdateDriverLicenseVerificationAsync(int userId, bool isVerified)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var success = await _userRepo.UpdateDriverLicenseVerificationAsync(userId, isVerified);
                response.Result = success;
                response.Message = success ? "Driver license verification updated successfully" : "User not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> UpdateCitizenIdVerificationAsync(int userId, bool isVerified)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var success = await _userRepo.UpdateCitizenIdVerificationAsync(userId, isVerified);
                response.Result = success;
                response.Message = success ? "Citizen ID verification updated successfully" : "User not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> UpdateUserDocumentsAsync(int userId, UpdateUserDocumentsDto dto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var success = await _userRepo.UpdateUserDocumentsAsync(
                    userId, 
                    dto.DriverLicenseNumber, 
                    dto.DriverLicenseImage, 
                    dto.CitizenId, 
                    dto.CitizenIdImage);
                
                response.Result = success;
                response.Message = success ? "User documents updated successfully" : "User not found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}
