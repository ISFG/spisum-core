using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models
{
    // Code lists static class
    public static class CodeLists
    {
        #region Nested Types, Enums, Delegates

        public static class DocumentTypes
        {
            #region Fields

            public static Dictionary<string, string> List = new Dictionary<string, string>
            {
                { "analog", "analogový" },
                { "digital", "digitální" }
            };

            public static string Analog = "analog";
            public static string Digital = "digital";

            #endregion
        }

        #endregion
    }
}
