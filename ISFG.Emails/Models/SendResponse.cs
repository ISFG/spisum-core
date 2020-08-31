using System;

namespace ISFG.Emails.Models
{
    public class SendResponse
    {
        #region Constructors

        public SendResponse(bool isSuccesfullySended, Exception exception = null)
        {
            IsSuccesfullySended = isSuccesfullySended;
            Exception = exception;
        }

        #endregion

        #region Properties

        public bool IsSuccesfullySended { get; set; }
        public Exception Exception { get; set; }

        #endregion
    }
}
