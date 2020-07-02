using System.Collections.Generic;
using System.Linq;
using ISFG.Common.Extensions;
using ISFG.SpisUm.Controllers.App.V1;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ISFG.SpisUm.Swagger
{
    public class GroupFilter : IOperationFilter
    {
        #region Fields

        private readonly string[] _controllerIgnoreList =
        {
            nameof(CodeListsController),
            nameof(DataboxController),
            nameof(EmailController),
            nameof(GroupController),
            nameof(NodeController),
            nameof(PathsController),
            nameof(SignerController),
            nameof(SignerAppController)
        };

        #endregion

        #region Implementation of IOperationFilter

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null) operation.Parameters = new List<OpenApiParameter>();

            if (IsControllerAllowed(context))
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Group",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "User group",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
        }

        #endregion

        #region Private Methods

        private bool IsControllerAllowed(OperationFilterContext context)
        {
            if (_controllerIgnoreList == null || !_controllerIgnoreList.Any())
                return true;
            
            if (context.ApiDescription.RelativePath.Contains("/admin/") || 
                context.ApiDescription.RelativePath.Contains("/auth/"))
                return false;
  
            var controllerType = context.ApiDescription.ActionDescriptor.As<ControllerActionDescriptor>().ControllerTypeInfo.ToString();

            return !_controllerIgnoreList.Any(x => controllerType.EndsWith(x));
        }

        #endregion
    }
}