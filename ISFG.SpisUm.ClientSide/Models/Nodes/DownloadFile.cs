using ISFG.Alfresco.Api.Models;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DownloadFile
    {
        #region Properties

        public string OrigFileName { get; set; }
        public FormDataParam File { get; set; }

        #endregion
    }
}
