using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.SearchApi;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/search")]
    public class SearchController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public SearchController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Search action via provided query
        /// </summary>
        [HttpPost]
        public async Task<ResultSetPaging> GetSearch([FromBody] SearchRequest body)
        {
            // Due to the bug in Alfresco, which will return 403 when permissions changes and search engine has delay,
            // there is a loop that will try to call it over and over if the search engine returns 200

            Exception lastException = null;
            var mustResponseUntil = DateTime.UtcNow.AddSeconds(20);

            while (DateTime.UtcNow <= mustResponseUntil)
                try
                {
                    return await _alfrescoHttpClient.Search(body);
                }
                catch (Exception e)
                {
                    lastException = e;
                    await Task.Delay(1000);
                    // Do nothing, keep in looping
                }

            if (lastException != null)
                throw lastException;
            
            throw new BadRequestException("", "Search engine encountered an error");
        }

        #endregion
    }
}
