using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Mail;

namespace OmnitakSupportHub.Services
{
    public class EmailService
    {

        private readonly string smtpServer = "smtp.gmail.com";
        private readonly int smtpPort = 587;
        private readonly string senderEmail = "rmmosehla23@gmail.com";
        private readonly string senderName = "OmnitakSupportHub";
        private readonly string smtpUsername = "rmmosehla23@gmail.com";
        private readonly string smtpPassword = "xsjo ybmr zumf rogu";

        public void SendRegistrationEmail(string toEmail, string toName)
        {
            var email = new MimeMessage();

            // Use private attributes instead of inline MailboxAddress
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));

            email.Subject = "Welcome to Omnitak Support Hub!";
            email.Body = new TextPart("html")
            {
                Text = $"<h2>Hello {toName}!</h2><p>Thank you for registering with us.</p>"
            };


            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                smtp.Connect(smtpServer, smtpPort, false);
                smtp.Authenticate(smtpUsername, smtpPassword);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }

        public void SendPasswordResetEmail(string toEmail, string toName, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));

            email.Subject = "OmnitakSupportHub - Password Reset Request";
            email.Body = new TextPart("html")
            {
                Text = $@"
            <h2>Hello {toName},</h2>
            <p>You requested a password reset. Click the link below to reset your password:</p>
            <p><a href='{resetLink}' style='font-size: 16px; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0;'>
                Reset Password
            </a></p>
            <p><strong>This link expires in 1 hour.</strong></p>
            <p>If you didn't request this, please ignore this email.</p>
            <hr>
            <p>Best regards,<br>OmnitakSupportHub Team</p>"
            };

            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                smtp.Connect(smtpServer, smtpPort, false);
                smtp.Authenticate(smtpUsername, smtpPassword);
                smtp.Send(email);
                smtp.Disconnect(true);
            }
        }
    }
}
