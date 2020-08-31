using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    public class CodeListFileContent
    {
        #region Properties

        public string Name { get; set; }
        public string Title { get; set; }
        public List<CodeListFileContentValue> Values { get; set; }

        #endregion
    }
    public class CodeListFileContentValue
    {
        #region Properties

        public string Name { get; set; }
        public string[] Authorities { get; set; }

        #endregion
    }    
}
