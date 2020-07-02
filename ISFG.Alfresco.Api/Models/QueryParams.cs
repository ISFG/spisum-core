namespace ISFG.Alfresco.Api.Models
{
    public class SimpleQueryParams
    {
        #region Properties

        /// <summary>
        /// Skip count
        /// </summary>
        public int SkipCount { get; set; } = 0;

        /// <summary>
        /// Max items
        /// </summary>
        public int MaxItems { get; set; } = 100;

        #endregion
    }

    public class BasicQueryParams : SimpleQueryParams
    {
        #region Properties

        /// <summary>
        /// Fields
        /// </summary>
        public string Fields { get; set; }

        /// <summary>
        /// Order by
        /// </summary>
        public string OrderBy { get; set; }

        #endregion
    }

    public class AdvancedBasicQueryParams : BasicQueryParams
    {
        #region Properties

        /// <summary>
        /// Include
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// Where
        /// </summary>
        public string Where { get; set; }

        #endregion
    }

    public class BasicNodeQueryParams : AdvancedBasicQueryParams
    {
        #region Properties

        /// <summary>
        /// Include source
        /// </summary>
        public bool? IncludeSource { get; set; }

        #endregion
    }

    public class BasicNodeQueryParamsWithRelativePath : BasicNodeQueryParams
    {
        #region Properties

        /// <summary>
        /// Relative path
        /// </summary>
        public string RelativePath { get; set; }

        #endregion
    }

    public class IncludeFieldsQueryParams
    {
        #region Properties

        /// <summary>
        /// Paths
        /// </summary>
        public string Fields { get; set; }

        /// <summary>
        /// Include
        /// </summary>
        public string Include { get; set; }

        #endregion
    }

    public class ContentQueryParams
    {
        #region Properties

        /// <summary>
        /// Attachment
        /// </summary>
        public bool? Attachment { get; set; }

        #endregion
    }
}
