using System.Collections.Generic;
using ISFG.Alfresco.Api.Models;

namespace ISFG.SpisUm.ClientSide.Models
{
    public class TransactionHistoryParameters
    {
        #region Constructors

        public TransactionHistoryParameters()
        {
        }

        public TransactionHistoryParameters(string message) : this(message, null) 
        {
        }

        public TransactionHistoryParameters(List<ObjectDifference> parameters) : this(null, parameters) 
        {
        }

        public TransactionHistoryParameters(string message, List<ObjectDifference> parameters)
        {
            Parameters = parameters;
            Message = message;
        }

        #endregion

        #region Properties

        public string Message { get; set; }
        public List<ObjectDifference> Parameters { get; set; }

        #endregion
    }
}