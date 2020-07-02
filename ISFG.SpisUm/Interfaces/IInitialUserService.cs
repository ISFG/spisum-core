using System.Threading.Tasks;

namespace ISFG.SpisUm.Interfaces
{
    public interface IInitialUserService
    {
        #region Public Methods

        Task CheckCreateGroupAndAddPerson(string userId, string groupName);

        #endregion
    }
}
