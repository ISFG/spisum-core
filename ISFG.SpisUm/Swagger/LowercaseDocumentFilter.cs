using System.Collections.Generic;
using ISFG.Common.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ISFG.SpisUm.Swagger
{

    public class LowercaseDocumentFilter : IDocumentFilter
    {
        #region Implementation of IDocumentFilter

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths;

            foreach (var path in paths)
                checkOperations(path.Value.Operations);
        }

        #endregion

        #region Private Methods

        private Dictionary<string, OpenApiEncoding> checkExtensions(IDictionary<string, OpenApiEncoding> encodings)
        {
            var newEncodings = new Dictionary<string, OpenApiEncoding>();
            foreach (var encoding in encodings)
                newEncodings.Add(encoding.Key.FirstCharToLower(), encoding.Value);
            return newEncodings;
        }

        private void checkOperations(IDictionary<OperationType, OpenApiOperation> operations)
        {
            foreach (var operation in operations)
            {
                checkParameters(operation.Value.Parameters);
                checkRequestBodyContent(operation.Value.RequestBody?.Content);
            }
        }

        private void checkParameters(IList<OpenApiParameter> parameters)
        {
            foreach (var parameter in parameters)
                parameter.Name = parameter.Name.FirstCharToLower();
        }

        private Dictionary<string, OpenApiSchema> checkProperties(IDictionary<string, OpenApiSchema> properties)
        {
            var newProperties = new Dictionary<string, OpenApiSchema>();
            foreach (var property in properties)
                newProperties.Add(property.Key.FirstCharToLower(), property.Value);
            return newProperties;
        }

        private void checkRequestBodyContent(IDictionary<string, OpenApiMediaType> contents)
        {
            if (contents == null)
                return;
            foreach (var content in contents)
            {
                if (content.Value.Encoding?.Count > 0)
                    content.Value.Encoding = checkExtensions(content.Value.Encoding);
                if (content.Value.Schema?.Properties?.Count > 0)
                    content.Value.Schema.Properties = checkProperties(content.Value.Schema.Properties);
                if (content.Value.Schema?.Required?.Count > 0)
                    content.Value.Schema.Required = checkRequired(content.Value.Schema?.Required);
            }
        }

        private HashSet<string> checkRequired(ISet<string> requiredList)
        {
            var newRequired = new HashSet<string>();
            foreach (var required in requiredList)
                newRequired.Add(required.FirstCharToLower());
            return newRequired;
        }

        #endregion
    }
}