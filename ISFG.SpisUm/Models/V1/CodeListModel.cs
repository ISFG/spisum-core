using System.Collections.Generic;

namespace ISFG.SpisUm.Models.V1
{
    public class CodeListModel
    {
        #region Properties

        public string Name { get; set; }
        public string Title { get; set; }
        public List<string> Values { get; set; }

        #endregion
    }
}
