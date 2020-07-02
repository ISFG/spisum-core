using ISFG.Common.Attributes;
using ISFG.DataBox.Api.Interfaces;

namespace ISFG.DataBox.Api.Configurations
{
    [Settings("DataBoxApi")]
    public class DataBoxApiConfiguration : IDataBoxApiConfiguration
    {
        #region Implementation of IDataBoxApiConfiguration

        public string Url { get; set; }

        #endregion
    }
}