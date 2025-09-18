using Microsoft.Extensions.Configuration;
using PalService.Interface;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PalService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var from = _config["EmailSettings:From"];
            var smtp = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            using var message = new MailMessage(from!, to, subject, htmlBody) { IsBodyHtml = true };
            using var client = new SmtpClient(smtp, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };
            await client.SendMailAsync(message);
        }
    }
}


