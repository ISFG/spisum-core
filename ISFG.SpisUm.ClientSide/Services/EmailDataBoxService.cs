using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.EmailDatabox;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class EmailDataBoxService : IEmailDataBoxService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IComponentService _componentService;
        private readonly IIdentityUser _identityUser;
        private readonly IMapper _mapper;
        private readonly INodesService _nodesService;

        #endregion

        #region Constructors

        public EmailDataBoxService(
            IAlfrescoHttpClient alfrescoHttpClient, 
            IIdentityUser identityUser, 
            IComponentService componentService, 
            IMapper mapper,
            INodesService nodesService
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _identityUser = identityUser;
            _componentService = componentService;
            _mapper = mapper;
            _nodesService = nodesService;
        }

        #endregion

        #region Implementation of IEmailDataBoxService

        public async Task DontRegister(DontRegister parameters, EmailOrDataboxEnum type)
        {
            var emlNode = await _alfrescoHttpClient.GetNodeInfo(parameters.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            if (parameters.Body.Reason.Length < 4)
                throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

            if (parameters.Body.Reason.Length > 30)
                parameters.Body.Reason = parameters.Body.Reason.Substring(0, 30);

            var body = new NodeBodyUpdate();
            var path = string.Empty;

            switch (type)
            {
                case EmailOrDataboxEnum.Email:

                    if (emlNode?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomEmail, StringComparison.OrdinalIgnoreCase) == false)
                        throw new BadRequestException("", "Node is not in Mailbox");


                    path = SpisumNames.Paths.MailRoomEmailNotRegistered;
                    break;

                case EmailOrDataboxEnum.Databox:
                    if (emlNode?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomDataBox, StringComparison.OrdinalIgnoreCase) == false)
                        throw new BadRequestException("", "Node is not in Databox");

                    path = SpisumNames.Paths.MailRoomDataBoxNotRegistered;
                    break;
            }

            body = new NodeBodyUpdate
            {
                Properties = new Dictionary<string, object>
                        {
                            { SpisumNames.Properties.DigitalDeliveryNotRegisteredReasion, parameters.Body.Reason }
                        }
            };

            await _alfrescoHttpClient.UpdateNode(parameters.NodeId, body);
            await _nodesService.MoveChildrenByPath(parameters.NodeId, path);
        }

        public async Task<NodeEntry> Register(NodeUpdate body, EmailOrDataboxEnum type)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(body.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            var archiveFolder = string.Empty;
            var nodeType = string.Empty;

            switch (type)
            {
                case EmailOrDataboxEnum.Email:
                    archiveFolder = SpisumNames.Paths.MailRoomEmailArchived;
                    nodeType = SpisumNames.NodeTypes.Email;
                    break;

                case EmailOrDataboxEnum.Databox:
                    archiveFolder = SpisumNames.Paths.MailRoomDataBoxArchived;
                    nodeType = SpisumNames.NodeTypes.DataBox;
                    break;
            }

            var pathRegex = new Regex($"({SpisumNames.Paths.MailRoomUnfinished})$", RegexOptions.IgnoreCase);

            if (pathRegex.IsMatch(nodeInfo?.Entry?.Path?.Name ?? ""))
                return await CreateArhiveAndMoveAllFiles(body, archiveFolder);
            throw new BadRequestException("", "Node is not in expected path");
        }

        #endregion

        #region Private Methods

        private async Task<NodeEntry> CreateArhiveAndMoveAllFiles(NodeUpdate body, string archiveFolder)
        {
            var alfrescoBody = _mapper.Map<NodeBodyUpdate>(body);

            AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry> request;

            // Move all originals to archive
            var originalFiles = await _alfrescoHttpClient.GetNodeParents(body.NodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.DigitalDeliveryDocumentsUnfinished}')", ParameterType.QueryString))
                    );

            await originalFiles?.List?.Entries?.ForEachAsync(async x =>
            {
                // Remove old association (Unifinished)
                await _alfrescoHttpClient.DeleteSecondaryChildren(x?.Entry?.Id, body.NodeId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.DigitalDeliveryDocumentsUnfinished}')", ParameterType.QueryString)));

                // Create new association
                await _alfrescoHttpClient.CreateNodeSecondaryChildren(x?.Entry?.Id, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.DigitalDeliveryDocuments,
                    ChildId = body.NodeId
                });

                // Move all attachments of the original file
                var attachments = await _alfrescoHttpClient.GetNodeSecondaryChildren(x?.Entry?.Id, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.DigitalDeliveryAttachments}')", ParameterType.QueryString)));

                attachments?.List?.Entries?.ForEachAsync(async attachment =>
                {
                    await _nodesService.MoveByPath(attachment?.Entry?.Id, archiveFolder);

                    await _alfrescoHttpClient.NodeLock(attachment?.Entry?.Id, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
                });

                await _nodesService.MoveByPath(x?.Entry?.Id, archiveFolder);

                await _alfrescoHttpClient.NodeLock(x?.Entry?.Id, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            });

            request = new AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry>(
               parameters => _alfrescoHttpClient.GetNodeSecondaryChildren(body.NodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Path}", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString))
            ));

            while (await request.Next())
            {
                var response = request.Response();
                if (!(response?.List?.Entries?.Count > 0))
                    break;

                foreach (var item in response.List.Entries.ToList())
                {
                    var updateBody = new NodeBodyUpdate();
                    
                    var personGroup = await GetCreateUserGroup();

                    updateBody.Permissions = _nodesService.SetPermissions(personGroup.GroupPrefix, _identityUser.Id, true).Permissions;

                    updateBody.Properties = new Dictionary<string, object>
                    {
                        { AlfrescoNames.ContentModel.Owner, _identityUser.Id },
                        { SpisumNames.Properties.Group, _identityUser.RequestGroup }
                    };
                    
                    await _alfrescoHttpClient.UpdateNode(item.Entry.Id, updateBody);
                }
            }

            // Set PID to archive
            await _nodesService.MoveByPath(body.NodeId, SpisumNames.Paths.MailRoomNotPassed);
            await _nodesService.MoveAllComponets(body.NodeId);

            await _nodesService.Update(body);

            return await _alfrescoHttpClient.GetNodeInfo(body.NodeId);
        }

        public async Task<PersonGroup> GetCreateUserGroup()
        {
            if (string.IsNullOrWhiteSpace(_identityUser.RequestGroup))
                throw new BadRequestException("", "User has not properties set");

            GroupEntry groupInfo = null;

            var personGroupName = $"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}";

            try
            {
                groupInfo = await _alfrescoHttpClient.GetGroup(personGroupName);
            }
            catch
            {
                throw new BadRequestException("", "User group not found");
            }

            var groupMembers = await _alfrescoHttpClient.GetGroupMembers(personGroupName);

            if (groupMembers?.List?.Entries?.ToList().Find(x => x.Entry?.Id == _identityUser.Id) == null)

                await _alfrescoHttpClient.CreateGroupMember(personGroupName, new GroupMembershipBodyCreate { Id = _identityUser.Id, MemberType = GroupMembershipBodyCreateMemberType.PERSON });

            return new PersonGroup { PersonId = _identityUser.Id, GroupId = groupInfo.Entry.Id, GroupPrefix = _identityUser.RequestGroup };

            #endregion
        }
    }
}