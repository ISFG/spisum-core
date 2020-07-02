using System.Threading.Tasks;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface ISystemLoginService
    {
        #region Public Methods

        Task<string> GetPassword(string username);

        Task<bool> IsUserExists(string username);

        #endregion
    }
}