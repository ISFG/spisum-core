namespace ISFG.Pdf.Models.ShreddingPlan
{
    public class ShreddingPlanColumns
    {
        public string FileMark { get; set; }
        public string FileMarkText { get; set; }
        public string RetentionMark { get; set; }
        public uint? Period { get; set; }
        public bool IsParent { get; set; }
    }
}