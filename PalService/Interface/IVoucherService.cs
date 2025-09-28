using PalService.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IVoucherService
    {
        Task<ResponseDto<int>> CreateVoucherAsync(CreateVoucherDto dto, int adminId);
        Task<ResponseDto<VoucherDto>> UpdateVoucherAsync(int voucherId, UpdateVoucherDto dto, int adminId);
        Task<ResponseDto<VoucherDto>> GetVoucherByIdAsync(int voucherId);
        Task<ResponseDto<List<VoucherDto>>> GetAllVouchersAsync();
        Task<ResponseDto<List<VoucherDto>>> GetActiveVouchersAsync();
        Task<ResponseDto<bool>> DeleteVoucherAsync(int voucherId, int adminId);
    }
}


