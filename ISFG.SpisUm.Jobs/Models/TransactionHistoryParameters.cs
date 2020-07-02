using System.Collections.Generic;
using ISFG.Alfresco.Api.Models;

namespace ISFG.SpisUm.Jobs.Models
{
    public class TransactionHistoryParameters
    {
        #region Properties

        public string Message { get; set; }
        public List<ObjectDifference> Parameters { get; set; }

        #endregion
    }
}