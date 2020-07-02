using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Interfaces;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class DocumentServiceExt
    {
        #region Static Methods

        public static async Task<NodeEntry> CreateDocument(this IDocumentService documentService, string nodePath = null) =>
            await documentService.Create(null, nodePath);

        #endregion
    }
}