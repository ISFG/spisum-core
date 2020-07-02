using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class SignerCreateValidator : AbstractValidator<SignerCreate>
    {
        #region Fields

        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public SignerCreateValidator(IAlfrescoHttpClient alfrescoHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.DocumentId);

                    return _nodeEntry?.Entry?.Id != null;
                })
                .WithName(x => nameof(x.DocumentId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() => 
                {
                    RuleFor(x => x.DocumentId)
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        .OnAnyFailure(x => throw new BadRequestException($"NodeId {x.DocumentId} must be type of {SpisumNames.NodeTypes.Document}"));
                });
            
            RuleFor(x => x)
                .Must(x => x.ComponentId != null && x.ComponentId.All(x => !string.IsNullOrEmpty(x)))
                .When(x => x != null)
                .OnAnyFailure(x => throw new BadRequestException("Component id's can't be null."));
            
            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.DocumentId))
                .When(x => x != null)
                .OnAnyFailure(x => throw new BadRequestException("Document id can't be null."));   
        }

        #endregion
    }
}