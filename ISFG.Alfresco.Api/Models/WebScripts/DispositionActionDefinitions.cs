namespace ISFG.Alfresco.Api.Models.WebScripts
{
    public class DispositionActionDefinitions
    {
        #region Properties

        public string Period { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string PeriodProperty { get; set; }
        public string CombineDispositionStepConditions { get; set; }
        public string EligibleOnFirstCompleteEvent { get; set; }
        public string GhostOnDestroy { get; set; }
        public string Description { get; set; }
        public string[] Events { get; set; }

        #endregion
    }
}
