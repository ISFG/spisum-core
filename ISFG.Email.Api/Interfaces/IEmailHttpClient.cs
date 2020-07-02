using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Email.Api.Models;
using ISFG.Emails.Models;
using Microsoft.AspNetCore.Http;

namespace ISFG.Email.Api.Interfaces
{
    public interface IEmailHttpClient
    {
        #region Public Methods

        Task<List<EmailAccount>> Accounts();
        Task<int> Refresh();
        Task<SendResponse> Send(string to, string from, string subject, string htmlBody, IEnumerable<IFormFile> attachments);
        Task<SendResponse> Send(string to, string from, string subject, string htmlBody, IEnumerable<FormDataParam> attachments);
        Task<EmailStatusResponse> Status(int id);

        #endregion
    }
}