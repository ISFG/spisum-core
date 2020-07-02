using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileToRepository
    {
        #region Properties

        public string Group { get; set; }
        public List<string> Ids { get; set; }

        #endregion
    }
}