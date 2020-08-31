using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IPersonService
    {
        #region Public Methods

        Task<PersonGroup> GetCreateUserGroup();

        #endregion
    }
}
