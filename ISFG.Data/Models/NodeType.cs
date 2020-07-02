using System.Collections.Generic;

namespace ISFG.Data.Models
{
    public class NodeType
    {
        #region Constructors

        public NodeType()
        {
            TransactionHistory = new HashSet<TransactionHistory>();
        }

        #endregion

        #region Properties

        public string Code { get; set; }
        public string Description { get; set; }

        public virtual ICollection<TransactionHistory> TransactionHistory { get; set; }

        #endregion
    }
}
