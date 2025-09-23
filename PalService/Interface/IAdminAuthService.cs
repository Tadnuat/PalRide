using PalService.DTOs;
using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IAdminAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDtos dto);
    }
}



