using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

public class EmailSenderStub : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Просто имитируем успешную отправку email
        return Task.CompletedTask;
    }
}
