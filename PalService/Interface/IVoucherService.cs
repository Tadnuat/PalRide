using PalService.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IVoucherService
    {
        Task<ResponseDto<int>> CreateVoucherAsync(CreateVoucherDto dto, int adminId);
    }
}


