using System;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileToRepositoryFileValidator : AbstractValidator<DocumentProperties>
    {
        #region Fields

        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public FileToRepositoryFileValidator(IAlfrescoHttpClient alfrescoHttpClient)
        {
             RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId);

                    return  _nodeEntry?.Entry?.Id != null;
                })
                .WithName("File")
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.File)
                        .OnAnyFailure(x => throw new Exception($"Provided nodeId must be NodeType of {SpisumNames.NodeTypes.File}"));
                });
        }

        #endregion
    }
}