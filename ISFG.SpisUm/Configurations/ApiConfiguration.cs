using System.Collections.Generic;
using ISFG.Common.Attributes;
using ISFG.SpisUm.Interfaces;

namespace ISFG.SpisUm.Configurations
{
    [Settings("Api")]
    public class ApiConfiguration : IApiConfiguration
    {
        #region Implementation of IApiConfiguration

        public IList<SwaggerOptions> SwaggerOptions { get; set; }

        public string Url { get; set; }

        #endregion
    }
    
    public class SwaggerOptions
    {
        #region Properties

        public string Route { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public IList<Versions> Versions { get; set; }

        #endregion
    }
    
    public class Versions
    {
        #region Properties

        public string Version { get; set; }
        public bool Enabled { get; set; }

        #endregion
    }
}
