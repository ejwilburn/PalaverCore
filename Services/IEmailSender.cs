using System.Threading.Tasks;

namespace Palaver.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
