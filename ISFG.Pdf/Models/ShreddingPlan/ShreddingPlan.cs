using System.Collections.Generic;

namespace ISFG.Pdf.Models.ShreddingPlan
{
    public class ShreddingPlan
    {
        public string Title { get; set; }
        public ShreddingPlanRows Rows { get; set; }
        public List<ShreddingPlanColumns> Columns { get; set; }
    }
}