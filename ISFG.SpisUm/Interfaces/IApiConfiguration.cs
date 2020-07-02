using System.Collections.Generic;
using ISFG.SpisUm.Configurations;

namespace ISFG.SpisUm.Interfaces
{
    public interface IApiConfiguration
    {
        #region Properties

        string Url { get; }
        IList<SwaggerOptions> SwaggerOptions { get; }

        #endregion
    }
}