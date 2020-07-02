using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Common.Extensions;
using ISFG.Common.HttpClient;
using ISFG.Email.Api.Interfaces;
using ISFG.Email.Api.Models;
using ISFG.Emails.Models;
using Microsoft.AspNetCore.Http;
using RestSharp;
using Serilog;
using Serilog.Events;

namespace ISFG.Email.Api.Services
{
    public class EmailHttpClient : HttpClient, IEmailHttpClient
    {
        #region Constructors

        public EmailHttpClient(IEmailApiConfiguration emailApiConfiguration) : base(emailApiConfiguration.Url)
        {
        }

        #endregion

        #region Implementation of IEmailHttpClient

        public async Task<List<EmailAccount>> Accounts() =>
            await ExecuteRequest<List<EmailAccount>>(Method.GET, "api/email/accounts");

        public async Task<int> Refresh() =>
            await ExecuteRequest<int>(Method.POST, "api/email/refresh");

        public async Task<SendResponse> Send(string to, string from, string subject, string htmlBody, IEnumerable<IFormFile> attachments)
        {
            List<FormDataParam> body = new List<FormDataParam>();

            await attachments.ForEachAsync(async x =>
            {
                body.Add(new FormDataParam(await x.GetBytes(), x.FileName, x.Name, x.ContentType));
            });

            return await Send(to, from, subject, htmlBody, body);
        }

        public async Task<SendResponse> Send(string to, string from, string subject, string htmlBody, IEnumerable<FormDataParam> attachments)
        {
            return await ExecuteRequest<SendResponse>(Method.POST, "api/email/send", attachments, ImmutableList<Parameter>.Empty
            .Add(new Parameter("to", to, ParameterType.QueryString))
            .Add(new Parameter("from", from, ParameterType.QueryString))
            .Add(new Parameter("subject", subject, ParameterType.QueryString))
            .Add(new Parameter("htmlbody", htmlBody, ParameterType.QueryString))
                );
        }

        public async Task<EmailStatusResponse> Status(int id) =>
            await ExecuteRequest<EmailStatusResponse>(Method.GET, $"api/email/status?id={id}");

        #endregion

        #region Override of HttpClient

        protected override void BuildContent(RestRequest request, object body)
        {
            if (body is List<FormDataParam> attachments)
            {
                attachments.ForEach(x => request.AddFileBytes("attachments", x.File, x.FileName, x.ContentType));
                return;
            }

            base.BuildContent(request, body);
        }

        protected override void LogHttpRequest(IRestResponse response)
        {
            if (!Log.IsEnabled(LogEventLevel.Debug))
                return;

            Log.Debug(response.ToMessage());
        }

        #endregion

        #region Nested Types, Enums, Delegates

        private class SendEmail
        {
            #region Properties

            public string To { get; set; }
            public string From { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
            public List<IFormFile> Attachments { get; set; }

            #endregion
        }

        #endregion
    }
}
