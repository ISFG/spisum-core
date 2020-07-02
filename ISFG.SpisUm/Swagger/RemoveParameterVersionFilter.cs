using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ISFG.SpisUm.Swagger
{
    public class RemoveParameterVersionFilter : IOperationFilter
    {
        #region Implementation of IOperationFilter

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.Single(p => p.Name == "version");
            operation.Parameters.Remove(versionParameter);
        }

        #endregion
    }
}