using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Emails.Models;
using Microsoft.AspNetCore.Http;

namespace ISFG.Emails.Interface
{
    public interface IEmailProvider
    {
        #region Public Methods

        Task<SendResponse> Send(string to, string subject, string htmlBody, List<IFormFile> attachments = null);
        bool Send(EmailMessage emailMessage);

        #endregion
    }
}
