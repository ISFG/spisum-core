using System.Threading.Tasks;

namespace ISFG.SpisUm.Interfaces
{
    public interface IInitialGroupService
    {
        #region Public Methods

        Task AddMainGroupMember(string mainGroup, string groupId);

        #endregion
    }
}
