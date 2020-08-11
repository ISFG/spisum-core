using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.TransactionHistory
{
    public class TransactionHistoryQuery
    {
        #region Properties

        [FromQuery]
        public string NodeId { get; set; }

        [FromQuery]
        public DateTime? From { get; set; }

        [FromQuery]
        public DateTime? To { get; set; }

        [Required]
        [FromQuery]
        public int CurrentPage { get; set; }

        [Required]
        [FromQuery]
        public int PageSize { get; set; }

        [FromQuery]
        public List<string> NodeIds { get; set; }

        #endregion
    }
}