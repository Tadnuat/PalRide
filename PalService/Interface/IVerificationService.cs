using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IVerificationService
    {
        Task<bool> IsDriverVerifiedAsync(int userId);
        Task<bool> IsPassengerVerifiedAsync(int userId);
        Task<bool> IsUserVerifiedForRoleAsync(int userId, string role);
        Task<string> GetVerificationErrorMessageAsync(int userId, string role);
    }
}

