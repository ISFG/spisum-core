using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Models.EmailDatabox;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IEmailDataBoxService
    {
        #region Public Methods

        public Task DontRegister(DontRegister parameters, EmailOrDataboxEnum type);
        public Task<NodeEntry> Register(NodeUpdate nodeId, EmailOrDataboxEnum type);

        #endregion
    }
}
