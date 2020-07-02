using System.Threading.Tasks;
using ISFG.Data.Models;

namespace ISFG.Data.Interfaces
{
    public interface ISystemLoginRepository
    {
        #region Public Methods

        Task<SystemLogin> CreateUser(string username, string password);
        Task<SystemLogin> GetUser(string username);

        #endregion
    }
}