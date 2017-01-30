/*
Copyright 2017, Marcus McKinnon, E.J. Wilburn, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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
        private readonly SmtpOptions _options;

        public AuthMessageSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(string emailAddress, string subject, string message)
        {
            // Plug in your email service here to send an email.
            MimeMessage email = new MimeMessage();
            email.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            email.To.Add(new MailboxAddress("", emailAddress));
            email.Subject = subject;
            BodyBuilder bodyBulder = new BodyBuilder();
            bodyBulder.HtmlBody = message;
            email.Body = bodyBulder.ToMessageBody();

            using (SmtpClient client = new SmtpClient())
            {
                await client.ConnectAsync(_options.Server,
                    _options.Port,
                    (_options.RequireTls ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto)).ConfigureAwait(false);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(email).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
        }
    }
}
