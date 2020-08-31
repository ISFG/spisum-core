using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.Rules
{
    // This model was created from share webscript. Not guaranteed 100% workings.
    // Be wary when using this model.
    public class CreateRuleARM
    {
        #region Properties

        public CreateRuleData Data { get; set; }

        #endregion
    }
    public class CreateRuleData
    {
        #region Properties

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> RuleType { get; set; }
        public bool Disabled { get; set; }
        public string Url { get; set; }

        #endregion
    }
}
