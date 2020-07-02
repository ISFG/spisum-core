using ISFG.Common.Attributes;
using ISFG.SpisUm.Interfaces;

namespace ISFG.SpisUm.Configurations
{
    [Settings("Cors")]
    public class CorsConfiguration : ICorsConfiguration
    {
        #region Implementation of ICorsConfiguration

        public string[] Origins { get; set; }

        #endregion
    }
}