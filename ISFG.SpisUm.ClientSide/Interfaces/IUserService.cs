using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApiFixed;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IUsersService
    {
        #region Public Methods

        Task<PersonEntryFixed> CreateUser(string id, string firstName, string email, string password);
        Task<PersonPagingFixed> GetUsers(BasicQueryParams queryParams);

        #endregion
    }
}