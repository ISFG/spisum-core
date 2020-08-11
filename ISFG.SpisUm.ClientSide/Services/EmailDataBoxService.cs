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

                    body = new NodeBodyUpdate
                    {
                        Properties = new Dictionary<string, object>
                        {
                            { SpisumNames.Properties.EmailNotRegisteredReason, parameters.Body.Reason }
                        }
                    };
                    path = SpisumNames.Paths.MailRoomEmailNotRegistered;
                    break;

                case EmailOrDataboxEnum.Databox:
                    if (emlNode?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomDataBox, StringComparison.OrdinalIgnoreCase) == false)
                        throw new BadRequestException("", "Node is not in Databox");

                    body = new NodeBodyUpdate
                    {
                        Properties = new Dictionary<string, object>
                        {
                            { SpisumNames.Properties.DataBoxNotRegisteredReason, parameters.Body.Reason }
                        }
                    };
                    path = SpisumNames.Paths.MailRoomDataBoxNotRegistered;
                    break;
            }

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
                return await CreateArhiveAndMoveAllFiles(body, archiveFolder, nodeType);
            throw new BadRequestException("", "Node is not in expected path");
        }

        #endregion

        #region Private Methods

        private async Task<NodeEntry> CreateArhiveAndMoveAllFiles(NodeUpdate body, string archiveFolder, string nodeType)
        {
            var copiedNodes = new List<NodeEntry>();
            var updatedNodes = new List<NodeEntry>();
            var connections = new List<Tuple<string, string>>();

            var alfrescoBody = _mapper.Map<NodeBodyUpdate>(body);
            var documentInfo = await _alfrescoHttpClient.UpdateNode(body.NodeId, alfrescoBody, ImmutableList<Parameter>.Empty
               .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            var parentFolderInfo = await _alfrescoHttpClient.GetNodeInfo(documentInfo.Entry.ParentId);

            // create folder in archive because copy folder violation
            var copyFolderInfo = await _nodesService.CreateFolder(archiveFolder);

            AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry> request;

            // copy and remove all associations - Copies .eml/.zfo and all it's children to the archive folder
            var entries = (await _alfrescoHttpClient.GetNodeChildren(body.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, 1, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(nodeType='{nodeType}')", ParameterType.QueryString))))
                ?.List?.Entries?.ToList();

            if (entries?.Count > 0)
            {
                var nodeId = entries[0]?.Entry?.Id;

                request = new AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry>(
                    parameters => _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, parameters)
                );

                while (await request.Next())
                {
                    var response = request.Response();
                    if (!(response?.List?.Entries?.Count > 0))
                        break;

                    foreach (var item in response.List.Entries.ToList())
                    {
                        await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, item.Entry.Id);
                        var copyInfo = await _alfrescoHttpClient.NodeCopy(item.Entry.Id, new NodeBodyCopy
                        {
                            TargetParentId = copyFolderInfo.Entry.Id
                        });
                        var properties = copyInfo.Entry.Properties.As<JObject>().ToDictionary();
                        var pid = properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();
                        await _alfrescoHttpClient.UpdateNode(copyInfo.Entry.Id, new NodeBodyUpdate()
                            .AddProperty(SpisumNames.Properties.Pid, null)
                            .AddProperty(SpisumNames.Properties.PidRef, pid)
                            .AddProperty(SpisumNames.Properties.Ref, item.Entry.Id));
                        copiedNodes.Add(copyInfo);
                    }
                }

                // Copy .eml / .zfo
                var copyEmlZfo = await _alfrescoHttpClient.NodeCopy(nodeId, new NodeBodyCopy
                {
                    TargetParentId = copyFolderInfo.Entry.Id
                });

                var propertiesEmlZfo = copyEmlZfo.Entry.Properties.As<JObject>().ToDictionary();
                var pidEmlZfo = propertiesEmlZfo.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();

                await _alfrescoHttpClient.UpdateNode(copyEmlZfo?.Entry?.Id, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.Pid, null)
                    .AddProperty(SpisumNames.Properties.PidRef, pidEmlZfo)
                    .AddProperty(SpisumNames.Properties.Ref, nodeId));

                copiedNodes.Add(copyEmlZfo);
            }

            // Take all files and move them to the new location 
            // find all children and rename + change node type
            request = new AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry>(
               parameters => _alfrescoHttpClient.GetNodeSecondaryChildren(body.NodeId, parameters)
            );

            while (await request.Next())
            {
                var response = request.Response();
                if (!(response?.List?.Entries?.Count > 0))
                    break;

                foreach (var item in response.List.Entries.ToList())
                {
                    if (item.Entry.NodeType == SpisumNames.NodeTypes.Component)
                        continue;

                    connections.Add(new Tuple<string, string>(item.Entry.Id, copiedNodes.FirstOrDefault(x => x.Entry.Name == item.Entry.Name)?.Entry?.Id));

                    var updateBody = new NodeBodyUpdate();

                    if (item.Entry.NodeType == AlfrescoNames.ContentModel.Content)
                        updateBody.NodeType = SpisumNames.NodeTypes.Component;
                    if (item.Entry.NodeType == SpisumNames.NodeTypes.Email)
                        updateBody.NodeType = SpisumNames.NodeTypes.EmailComponent;
                    if (item.Entry.NodeType == SpisumNames.NodeTypes.DataBox)
                        updateBody.NodeType = SpisumNames.NodeTypes.DataBoxComponent;

                    var personGroup = await GetCreateUserGroup();

                    updateBody.Permissions = _nodesService.SetPermissions(personGroup.GroupPrefix, _identityUser.Id, true).Permissions;

                    updateBody.Properties = new Dictionary<string, object>
                    {
                        { AlfrescoNames.ContentModel.Owner, _identityUser.Id },
                        { SpisumNames.Properties.Group, _identityUser.RequestGroup }
                    };
                    
                    updatedNodes.Add(await _alfrescoHttpClient.UpdateNode(item.Entry.Id, updateBody));

                    if (item.Entry.NodeType == AlfrescoNames.ContentModel.Content)
                    {
                        
                    }
                }
            }

            // Set PID to archive
            List<NodeEntry> movedNodes = new List<NodeEntry>();
            movedNodes.Add(await _nodesService.MoveByPath(body.NodeId, SpisumNames.Paths.MailRoomNotPassed));
            movedNodes.AddRange(await _nodesService.MoveAllComponets(body.NodeId));

            foreach (var copy in copiedNodes)
            {
                var id = connections.First(x => x.Item2 == copy.Entry.Id);

                var node = movedNodes.FirstOrDefault(x => x.Entry.Id == id.Item1);

                if (node == null)
                    continue;

                var properties = node.Entry.Properties.As<JObject>().ToDictionary();
                var pid = properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();

                if (string.IsNullOrWhiteSpace(pid))
                    continue;

                await _alfrescoHttpClient.UpdateNode(copy.Entry.Id, new NodeBodyUpdate
                {
                    Properties = new Dictionary<string, object>
                    {
                        { SpisumNames.Properties.PidArchive, pid }
                    }
                });
            }

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