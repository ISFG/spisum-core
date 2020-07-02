using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.Rules
{
    // This model was created from share webscript. Not guaranteed 100% workings.
    // Be wary when using this model unless you are brave developer that seeks suffering
    public class CreateRuleAM
    {
        #region Properties

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool ApplyToChildren { get; set; }
        public bool Disabled { get; set; }
        public bool ExecuteAsynchronously { get; set; }
        public List<string> RuleType { get; set; } = new List<string>();
        public Action Action { get; set; } = new Action();

        #endregion
    }
}
