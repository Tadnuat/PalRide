using PalService.DTOs;
using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IAdminUserService
    {
        Task<ResponseDto<bool>> UpdateDriverLicenseVerificationAsync(int userId, bool isVerified);
        Task<ResponseDto<bool>> UpdateCitizenIdVerificationAsync(int userId, bool isVerified);
        Task<ResponseDto<bool>> UpdateUserDocumentsAsync(int userId, UpdateUserDocumentsDto dto);
    }
}
