using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IAuthorizationService
    {
        #region Public Methods

        Task<Authorization> Login(string username, string password);

        #endregion
    }
}