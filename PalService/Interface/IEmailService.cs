using System.Threading.Tasks;

namespace PalService.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }
}


