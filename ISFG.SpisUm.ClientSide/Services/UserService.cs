using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.SpisUm.ClientSide.Interfaces;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class UserService : IUsersService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public UserService(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Implementation of IUsersService

        public Task<PersonEntryFixed> CreateUser(string id, string firstName, string email, string password)
        {
            return _alfrescoHttpClient.CreatePerson(new PersonBodyCreate
            {
                Id = id,
                FirstName = firstName,
                Email = email,
                Password = password
            });
        }

        public Task<PersonPagingFixed> GetUsers(BasicQueryParams queryParams)
        {
            return _alfrescoHttpClient.GetPeople(ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .AddQueryParams(queryParams));
        }

        #endregion
    }
}