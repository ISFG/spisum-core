using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Email.Api.Interfaces;
using ISFG.Email.Api.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentCreateEmailValidator : AbstractValidator<ShipmentCreateEmail>
    {
        #region Fields

        private List<EmailAccount> _accounts;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ShipmentCreateEmailValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, IEmailHttpClient emailHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    try { _accounts = await emailHttpClient.Accounts(); } catch { }

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _accounts != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x)
                        .Must(x => _accounts.Count > 0)
                        .WithMessage(x => "No email configuration on email server");

                    RuleFor(x => x.Body.Sender)
                        .Must(y => _accounts?.Any(x => x.Username == y) ?? false)                        
                        .WithMessage(x => "Sender was not found in email configuration on email server");
                });

            RuleFor(x => x.Body.Recipient)
                        .Must(x => EmailUtils.IsValidEmail(x))
                        .When(x => x.Body != null)
                        .WithName(x => "Recipient")
                        .WithMessage("Recipient is not a valid email address");

            RuleFor(x => x.Body.Subject)
                        .Must(x => CheckSubjectLenght(x))
                        .When(x => x.Body != null)
                        .WithName(x => "Subject")
                        .WithMessage("Subject is too long");

            RuleFor(x => x.Body.Recipient)
                        .Must(x => CheckRecipientLenght(x))
                        .When(x => x.Body != null)
                        .WithName(x => "Recipient")
                        .WithMessage("Recipient is too long");

            RuleFor(x => x.Body.Components)
                        .Must(y => y?.Count > 0)
                        .When(x => x.Body != null)
                        .WithName(x => "Components")
                        .WithMessage("Components cannot be empty");
        }

        #endregion

        #region Private Methods

        private bool CheckRecipientLenght(string subject)
        {
            return !(subject?.Length >= 254);
        }

        private bool CheckSubjectLenght(string subject)
        {
            return !(subject?.Length >= 255);
        }

        #endregion
    }
}
