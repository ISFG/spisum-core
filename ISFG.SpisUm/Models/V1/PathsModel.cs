using System.Collections.Generic;

namespace ISFG.SpisUm.Models.V1
{
    public class PathsModel
    {
        #region Properties

        public List<PathsModel> Childs { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> Permissions { get; set; }

        #endregion
    }
}