using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.Rules
{
    public class GetNodeRulesARM
    {
	    #region Properties

	    public List<GetRuleEntry> Data { get; set; }

	    #endregion
    }

	public class GetRuleEntry
	{
		#region Properties

		public string Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public string[] RuleType { get; set; }
		public bool Disabled { get; set; }
		public ActionInfo Action { get; set; }
		public RuleOwningNode OwningNode { get; set; }
		public string Url { get; set; }

		#endregion
	}
	public class RuleOwningNode
	{
		#region Properties

		public string NodeRef { get; set; }
		public string Name { get; set; }

		#endregion
	}
}