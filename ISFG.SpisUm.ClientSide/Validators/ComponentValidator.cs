using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class ComponentValidator : AbstractValidator<DocumentProperties>
    {
        #region Fields

        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ComponentValidator(IAlfrescoHttpClient alfrescoHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId);

                    return _nodeEntry?.Entry?.Id != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() => 
                {
                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Component)
                        .OnAnyFailure(x => throw new BadRequestException($"NodeId {x.NodeId} must be type of {SpisumNames.NodeTypes.Component}"));
                });
        }

        #endregion
    }
}