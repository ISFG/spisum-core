using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IConceptService
    {
        #region Public Methods

        Task<NodeEntry> ToDocument(string conceptId, string authorId, string subject, int attachmentCount, GenerateSsid ssidConfiguration, DateTime? settleTo = null);

        #endregion
    }
}
