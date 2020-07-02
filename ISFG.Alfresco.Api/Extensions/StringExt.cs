using ISFG.Common.Extensions;

namespace ISFG.Alfresco.Api.Extensions
{
    public static class StringExt
    {
        #region Fields

        private const string nodeRefPrefix = "workspace://SpacesStore/";

        #endregion

        #region Static Methods

        public static string ToAlfrescoAuthentication(this string str) =>
            str != null ? $"ROLE_TICKET:{str}".ToBase64() : string.Empty;

        /// <summary>
        /// Removes Alfresco node prefix <b>"workspace://SpacesStore/"</b>> from the string.
        /// </summary>                
        public static string ToNodeId(this string str) => str.Replace(nodeRefPrefix, string.Empty);

        #endregion
    }
}