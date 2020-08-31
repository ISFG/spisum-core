using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Common.Extensions;
using ISFG.Emails.Interface;
using ISFG.Emails.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using MimeKit;

namespace ISFG.Emails
{
    public class EmailProvider : IEmailProvider
    {
        #region Fields

        private readonly IEmailConfiguration _emailConfiguration;

        #endregion

        #region Constructors

        public EmailProvider(IEmailConfiguration emailConfiguration)
        {
            _emailConfiguration = emailConfiguration;
        }

        #endregion

        #region Implementation of IEmailProvider

        /// <summary>
        /// Sends e-mail message
        /// </summary>
        /// <param name="to">Email adress which emaill message will be delivered to.</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="htmlBody">Body of email in HTML format</param>
        /// <param name="attachments">Files that will be send with the email</param>
        /// <returns>true if email was succefully sended otherwise false</returns>
        public async Task<SendResponse> Send(string to, string subject, string htmlBody, List<IFormFile> attachments = null)
        {
            try
            {                
                to = to.Trim();
                subject = subject.Trim();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailConfiguration.DisplayName, _emailConfiguration.Username));
                message.To.Add(new MailboxAddress(to));
                message.Subject = subject;

                var builder = new BodyBuilder();
                if (htmlBody != null)
                    builder.HtmlBody = htmlBody;

                if (attachments != null)
                    foreach (var attachment in attachments) builder.Attachments.Add(attachment.FileName, await attachment.GetBytes());

                message.Body = builder.ToMessageBody();

                //Be careful that the SmtpClient class is the one from Mailkit not the framework!
                using (var emailClient = new SmtpClient())
                {
                    //The last parameter here is to use SSL (Which you should!)
                    emailClient.Connect(_emailConfiguration.Stmp.Host, _emailConfiguration.Stmp.Port, _emailConfiguration.Stmp.UseSSL);

                    //Remove any OAuth functionality as we won't be using it. 
                    emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                    emailClient.Authenticate(_emailConfiguration.Username, _emailConfiguration.Password);

                    emailClient.Send(message);

                    emailClient.Disconnect(true);

                    return new SendResponse(true);
                }
            }
            catch (Exception e)
            {
                return new SendResponse(false, e);
            }
        }


        public bool Send(EmailMessage emailMessage)
        {
            var message = new MimeMessage();
            message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

            message.Subject = emailMessage.Subject;
            //We will say we are sending HTML. But there are options for plaintext etc. 
            //message.Body = new TextPart(TextFormat.Html)
            //{
            //	Text = emailMessage.Content
            //};
            var builder = new BodyBuilder();
            if (emailMessage.HtmlContent != null)
                builder.HtmlBody = emailMessage.HtmlContent;
            if (emailMessage.Attachments != null)
                foreach (Attachment attachment in emailMessage.Attachments) builder.Attachments.Add(attachment.FileName, attachment.File);

            message.Body = builder.ToMessageBody();

            //Be careful that the SmtpClient class is the one from Mailkit not the framework!
            using (var emailClient = new SmtpClient())
            {
                //The last parameter here is to use SSL (Which you should!)
                emailClient.Connect(_emailConfiguration.Stmp.Host, _emailConfiguration.Stmp.Port, true);

                //Remove any OAuth functionality as we won't be using it. 
                emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

                emailClient.Authenticate(_emailConfiguration.Username, _emailConfiguration.Password);

                emailClient.Send(message);

                emailClient.Disconnect(true);

                return true;
            }
        }

        #endregion
    }
}
