using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.Sites
{
    public class ShreddingPlanModel
    {
        #region Properties

        public string Id { get; set; }
        public List<ShreddingPlanItemModel> Items { get; set; }
        public string Name { get; set; }

        #endregion
    }

    public class ShreddingPlanItemModel
    {
        #region Properties

        /// <summary>
        /// true = is subject group caption
        /// </summary>
        public bool? IsCaption { get; set; }

        /// <summary>
        /// File mark, like "51.1", "51.5.1", ...
        /// </summary>
        public string FileMark { get; set; }

        /// <summary>
        /// Parent's FileMark
        /// </summary>
        public string ParentFileMark { get; set; }

        /// <summary>
        /// Period in years
        /// </summary>
        public uint? Period { get; set; }

        /// <summary>
        /// Allowed values A|V|S
        /// </summary>
        public string RetentionMark { get; set; }

        /// <summary>
        /// Caption of item
        /// </summary>
        public string SubjectGroup { get; set; }

        /// <summary>
        /// Custom trigger action
        /// </summary>
        public uint? TriggerActionId { get; set; }

        #endregion
    }
}
