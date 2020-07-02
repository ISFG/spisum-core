namespace ISFG.Exceptions.Models
{
    internal class ExceptionModel
    {
        #region Properties

        public string Type { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
        public string Stack { get; set; }

        #endregion
    }
}