using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace Ice_Cream_Parlour_Eproject.Services.Email
{
    /// <summary>
    /// Sends transactional e-mail through SMTP. When SMTP is not configured the
    /// message (including password-reset links) is written to the log instead of
    /// failing the request, which keeps development and demo deployments usable.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogWarning(
                    "SMTP is not configured. E-mail to {Email} with subject '{Subject}' was not sent. Body: {Body}",
                    email, subject, htmlMessage);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await client.SendMailAsync(message);
        }
    }
}
