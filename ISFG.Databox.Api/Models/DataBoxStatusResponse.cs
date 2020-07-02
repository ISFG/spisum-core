namespace ISFG.DataBox.Api.Models
{
    public class DataboxAccount
    {
        #region Properties

        public string Name { get; set; }
        public string Username { get; set; }

        #endregion
    }

    public class DataBoxStatusResponse
    {
        #region Properties

        public int JobId { get; set; }
        public bool Running { get; set; }
        public int NewMessageCount { get; set; }

        #endregion
    }
}