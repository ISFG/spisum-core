using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentToRepository
    {
        #region Properties

        public string Group { get; set; }
        public List<string> Ids { get; set; }

        #endregion
    }
}