using ISFG.Common.Attributes;
using ISFG.Email.Api.Interfaces;

namespace ISFG.Email.Api.Configurations
{
    [Settings("EmailApi")]
    public class EmailApiConfiguration : IEmailApiConfiguration
    {
        #region Implementation of IEmailApiConfiguration

        public string Url { get; set; }

        #endregion
    }
}