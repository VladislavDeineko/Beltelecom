using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailSender : IEmailSender
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "jeb222221@gmail.com";
    private readonly string _smtpPass = "wchbibruxhdsgqbc"; // Пароль от почты
    private readonly string _adminEmail = "jeb222221@gmail.com"; // Ваша почта для получения уведомлений

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using (var client = new SmtpClient(_smtpServer, _smtpPort))
        {
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
            client.EnableSsl = true;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }

    public async Task SendFeedbackToAdminAsync(string name, string email, string message)
    {
        string subject = "Новое сообщение с формы обратной связи";
        string htmlMessage = $@"
            <html>
                <body>
                    <h2 style='color: #2c3e50;'>Новое сообщение с сайта BeltelecomShop</h2>
                    <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                        <p><strong style='color: #3498db;'>От кого:</strong> {name} ({email})</p>
                        <p><strong style='color: #3498db;'>Сообщение:</strong></p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #3498db; margin: 10px 0;'>
                            {message}
                        </div>
                        <p style='font-size: 0.9em; color: #7f8c8d;'>
                            Это сообщение было отправлено с формы обратной связи вашего сайта.
                        </p>
                    </div>
                </body>
            </html>";

        await SendEmailAsync(_adminEmail, subject, htmlMessage);
    }
}