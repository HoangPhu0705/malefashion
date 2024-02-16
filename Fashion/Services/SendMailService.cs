using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}

public class SendMailService : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SendMailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
        {
            client.Credentials = new NetworkCredential(_configuration["MailSettings:Email"], _configuration["MailSettings:Password"]);
            client.EnableSsl = true;

            using (MailMessage mailMessage = new MailMessage("phuhoang07051003@gmail.com", email))
            {
                mailMessage.Subject = subject;
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = message;

                try
                {
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}

