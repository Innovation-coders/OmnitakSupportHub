using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpOptions)
        {
            _smtpSettings = smtpOptions.Value;
        }

        public void SendRegistrationEmail(string toEmail, string toName)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = "Welcome to Omnitak Support Hub!";
            email.Body = new TextPart("html")
            {
                Text = $"<h2>Hello {toName}!</h2><p>Thank you for registering with us.</p>"
            };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect(_smtpSettings.Server, _smtpSettings.Port, false);
                smtp.Authenticate(_smtpSettings.Username, _smtpSettings.Password);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }

        public void SendPasswordResetEmail(string toEmail, string toName, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = "OmnitakSupportHub - Password Reset Request";

            email.Body = new TextPart("html")
            {
                Text = $@"
                <h2>Hello {toName},</h2>
                <p>You requested a password reset. Click the link below to reset your password:</p>
                <p><a href='{resetLink}' style='font-size: 16px; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0;'>Reset Password</a></p>
                <p><strong>This link expires in 1 hour.</strong></p>
                <p>If you didn't request this, please ignore this email.</p>
                <hr>
                <p>Best regards,<br>OmnitakSupportHub Team</p>"
            };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect(_smtpSettings.Server, _smtpSettings.Port, false);
                smtp.Authenticate(_smtpSettings.Username, _smtpSettings.Password);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }
    }
}
