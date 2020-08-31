using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.Rules
{

    #region RuleInfo
    public class ActionInfo : Action
    {
        #region Properties

        public string Url { get; set; }

        #endregion
    }
    public class ActionsInfo : Actions
    {
        #region Properties

        public string Id { get; set; }
        public bool ExecuteAsync { get; set; }
        public string Url { get; set; }

        #endregion
    }
    public class ConditionsInfo : Conditions
    {
        #region Properties

        public string Id { get; set; }
        public string Url { get; set; }

        #endregion
    }
    #endregion

    #region RuleCreate
    public class Action
    {
        #region Properties

        public string ActionDefinitionName { get; set; }
        public List<Conditions> Conditions { get; set; } = new List<Conditions>();
        public List<Actions> Actions { get; set; } = new List<Actions>();

        #endregion
    }
    public class Actions
    {
        #region Properties

        public string ActionDefinitionName { get; set; }
        public ActionParameterValues ParameterValues { get; set; }

        #endregion
    }
    public class Conditions
    {
        #region Properties

        public string ConditionDefinitionName { get; set; }

        public ConditionParameterValues ParameterValues { get; set; }

        // InvertCondition = true means don't invert. If you wonder why, ask Alfresco
        public bool? InvertCondition { get; set; } = null;

        #endregion
    } 
    #endregion

    public class ActionParameterValues
    {
        #region Properties

        [JsonProperty("script-ref")]
        public string ScriptRef { get; set; }

        #endregion
    }
    public class ConditionParameterValues
    {
        #region Properties

        public string Operation { get; set; }
        public string Value { get; set; }
        public string Property { get; set; }

        #endregion
    }
}
