using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ISFG.SpisUm.Swagger
{
    public class ReplaceValueVersionFilter : IDocumentFilter
    {
        #region Implementation of IDocumentFilter

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = new OpenApiPaths();
            foreach (var path in swaggerDoc.Paths)
                paths.Add(path.Key.Replace("v{version}", swaggerDoc.Info.Version), path.Value);
            swaggerDoc.Paths = paths;
        }

        #endregion
    }
}