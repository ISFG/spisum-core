﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Common.Extensions;
using ISFG.Common.HttpClient;
using ISFG.DataBox.Api.Interfaces;
using ISFG.DataBox.Api.Models;
using RestSharp;
using Serilog;
using Serilog.Events;

namespace ISFG.DataBox.Api.Services
{
    public class DataBoxHttpClient : HttpClient, IDataBoxHttpClient
    {
        #region Constructors

        public DataBoxHttpClient(IDataBoxApiConfiguration dataBoxApiConfiguration) : base(dataBoxApiConfiguration.Url)
        {
        }

        #endregion

        #region Implementation of IDataBoxHttpClient

        public async Task<List<DataboxAccount>> Accounts() =>
            await ExecuteRequest<List<DataboxAccount>>(Method.GET, "accounts");

        public async Task<int> Refresh() => 
            await ExecuteRequest<int>(Method.POST, "refresh");

        public async Task<DataBoxSendResponse> Send(DataBoxSend input)
        {
            return await ExecuteRequest<DataBoxSendResponse>(Method.POST, "databox-message", input.Files, ImmutableList<Parameter>.Empty
            .Add(new Parameter("subject", input.Subject, ParameterType.QueryString))
            .Add(new Parameter("recipientId", input.RecipientId, ParameterType.QueryString))
            .Add(new Parameter("senderName", input.SenderId, ParameterType.QueryString))
            .Add(new Parameter("body", input.Body, ParameterType.QueryString))
                );
        }

        public async Task<DataBoxStatusResponse> Status(int id) => 
            await ExecuteRequest<DataBoxStatusResponse>(Method.GET, $"status?id={id}");

        #endregion

        #region Override of HttpClient

        protected override void BuildContent(RestRequest request, object body)
        {
            if (body is List<FormDataParam> attachments)
            {
                attachments.ForEach(x => request.AddFileBytes("files", x.File, x.FileName, x.ContentType));
                return;
            }

            base.BuildContent(request, body);
        }
        protected override void LogHttpRequest(IRestResponse response)
        {
            if (!Log.IsEnabled(LogEventLevel.Debug))
                return;

            try 
            {
                Log.Debug(response?.ToMessage());
            } 
            catch (Exception ex)
            {
                Log.Error(ex, "Something went with databox log http request message");
            }
        }

        #endregion
    }
}