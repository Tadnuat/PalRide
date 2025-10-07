using PalService.DTOs;

namespace PalService.Interface
{
    public interface IWalletService
    {
        Task<ResponseDto<DriverWalletSummaryDto>> GetDriverWalletSummaryAsync(int driverId);
    }
}











