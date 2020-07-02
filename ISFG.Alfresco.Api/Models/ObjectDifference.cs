namespace ISFG.Alfresco.Api.Models
{
    public class ObjectDifference
    {
        #region Constructors

        public ObjectDifference(Operations operation, string key, object newValue = null, object oldValue = null)
        {
            Operation = operation;
            Key = key;
            NewValue = newValue?.ToString();
            OldValue = oldValue?.ToString();
        }

        #endregion

        #region Properties

        public Operations Operation { get; set; }
        public string Key { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        #endregion
    }
    
    public enum Operations
    {
        Edit,
        New,
        Deleted
    }
}