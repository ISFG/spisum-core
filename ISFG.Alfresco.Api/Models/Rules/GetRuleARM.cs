﻿namespace ISFG.Alfresco.Api.Models.Rules
{
    public class GetRuleARM
    {
        #region Properties

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RuleType { get; set; }
        public bool ApplyToChildren { get; set; }
        public bool ExecuteAsynchronously { get; set; }
        public bool Disabled { get; set; }
        public string Url { get; set; }
        public ActionInfo Action { get; set; }
        public RuleOwningNode OwningNode { get; set; }

        #endregion
    }
}
