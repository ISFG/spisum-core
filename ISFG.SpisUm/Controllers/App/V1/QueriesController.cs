using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Models.Query;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/queries")]
    public class QueriesController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public QueriesController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Find people
        /// </summary>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        [HttpGet("people")]
        public async Task<PersonPaging> GetPeople([FromQuery] GetPeopleModel queryParams)
        {
            return await _alfrescoHttpClient.GetQueriesPeople(ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Term, queryParams.Term, ParameterType.QueryString))
                .AddQueryParams(queryParams));
        }

        #endregion
    }
}
