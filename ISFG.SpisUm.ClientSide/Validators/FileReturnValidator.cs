using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.File;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    internal class FileReturnValidator : AbstractValidator<FileReturn>
    {
        #region Fields

        private string _borrowedGroup;
        private string _borrowedUser;
        private string _form;
        private string _group;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public FileReturnValidator(IAlfrescoHttpClient alfrescoHttpClient,
                                   IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    var documentProperties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                    _borrowedUser = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Borrower)?.ToString();

                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    try
                    {
                        var properties = _nodeEntry?.Entry?.Properties.As<JObject>().ToDictionary();
                        _form = properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();
                        _borrowedGroup = properties.GetNestedValueOrDefault(SpisumNames.Properties.BorrowGroup)?.ToString();
                        _group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();
                    }
                    catch
                    {
                        // Just to be safe
                    }

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x)
                        .Must(y =>
                        {
                            if ((_form == SpisumNames.Form.Analog || _form == SpisumNames.Form.Hybrid) && identityUser.RequestGroup == _group)
                                return true;
                            if (_form == SpisumNames.Form.Digital && (identityUser.RequestGroup == _group || identityUser.RequestGroup == _borrowedGroup))
                                return true;
                            return false;
                        })
                        .WithName(x => "User")
                        .WithMessage("User isn't member of group that can return this file.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.File)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.File}.");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.RepositoryRented,
                            StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("File must be in repository site."));

                    RuleFor(x => x)
                        .Must(x => !string.IsNullOrWhiteSpace(_borrowedUser))
                        .WithMessage("File is not borrowed");

                });
        }

        #endregion
    }
}