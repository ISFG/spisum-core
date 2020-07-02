namespace ISFG.DataBox.Api.Models
{
    public class DataBoxSendResponse
    {
        #region Constructors

        public DataBoxSendResponse(bool isSuccesfullySended, string Exception = null)
        {
            IsSuccessfullySent = isSuccesfullySended;
        }

        #endregion

        #region Properties

        public bool IsSuccessfullySent { get; set; }
        public string MessageId { get; set; }
        public string Exception { get; set; }

        #endregion
    }
}
