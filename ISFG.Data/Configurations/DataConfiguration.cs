using ISFG.Common.Attributes;
using ISFG.Data.Interfaces;

namespace ISFG.Data.Configurations
{
    [Settings("Database")]
    public class DataConfiguration : IDataConfiguration
    {
        #region Implementation of IDataConfiguration

        public string Connection { set; get; }

        #endregion
    }
}