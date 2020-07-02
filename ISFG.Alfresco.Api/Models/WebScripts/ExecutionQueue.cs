using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.WebScripts
{
    public class ExecutionQueue
    {
        #region Properties

        public string NodeRef { get; set; }
        public IEnumerable<string> NodeRefs { get; set; }
        public string Name { get; set; }
        public ExecutionQueueParams Params { get; set; }

        #endregion
    }

    public class ExecutionQueueParams
    {
        #region Properties

        public ExecutionQueueParamsAsOfDate AsOfDate { get; set; }

        #endregion
    }

    public class ExecutionQueueParamsAsOfDate
    {
        #region Properties

        public string Iso8601 { get; set; }

        #endregion
    }
}
