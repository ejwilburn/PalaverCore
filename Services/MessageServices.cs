using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace Palaver.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender
    {
        public async Task SendEmailAsync(string emailAddress, string subject, string message)
        {
            // Plug in your email service here to send an email.
            MimeMessage email = new MimeMessage();
            email.From.Add(new MailboxAddress("E.J. Wilburn", "ejwilburn@gmail.com"));
            email.To.Add(new MailboxAddress("", emailAddress));
            email.Subject = subject;
            email.Body = new TextPart("plain") { Text = message };

            using (SmtpClient client = new SmtpClient())
            {
                client.LocalDomain = "killeverything.com";
                await client.ConnectAsync("minilinux", 25, SecureSocketOptions.StartTls).ConfigureAwait(false);
                await client.SendAsync(email).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
        }
    }
}
