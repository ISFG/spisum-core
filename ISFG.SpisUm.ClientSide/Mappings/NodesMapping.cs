using AutoMapper;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Mappings
{
    public class NodesMapping : Profile
    {
        #region Constructors

        public NodesMapping()
        {
            //----------- UPDATE NODE -------------//
            CreateMap<NodeEntry, NodeUpdateResponse>()
                .ForMember(d => d.Id, s => s.MapFrom(m => m.Entry.Id))
                .ForMember(d => d.Name, s => s.MapFrom(m => m.Entry.Name))
                .ForMember(d => d.NodeType, s => s.MapFrom(m => m.Entry.NodeType));
            
            CreateMap<NodeUpdate, NodeBodyUpdate>()
                .ForMember(d => d.Name, s => s.MapFrom(m => m.Body.Name))
                .ForMember(d => d.AspectNames, s => 
                {
                    s.Condition(x => x.Body.AspectNames != null);
                    s.MapFrom(x => x.Body.AspectNames);
                })
                .ForMember(d => d.NodeType, s => s.MapFrom(m => m.Body.NodeType))
                .ForMember(d => d.Properties, s => s.MapFrom(m => m.Body.Properties));
            
            CreateMap<NodeChildAssociationEntry , NodeEntry>()
                .ForPath(d => d.Entry.Id, s => s.MapFrom(m => m.Entry.Id))
                .ForPath(d => d.Entry.Name, s => s.MapFrom(m => m.Entry.Name))
                .ForPath(d => d.Entry.Path, s => s.MapFrom(m => m.Entry.Path))
                .ForPath(d => d.Entry.Permissions, s => s.MapFrom(m => m.Entry.Permissions))
                .ForPath(d => d.Entry.Content, s => s.MapFrom(m => m.Entry.Content))
                .ForPath(d => d.Entry.Properties, s => s.MapFrom(m => m.Entry.Properties))
                .ForPath(d => d.Entry.AllowableOperations, s => s.MapFrom(m => m.Entry.AllowableOperations))
                .ForPath(d => d.Entry.AspectNames, s => s.MapFrom(m => m.Entry.AspectNames))
                .ForPath(d => d.Entry.CreatedAt, s => s.MapFrom(m => m.Entry.CreatedAt))
                .ForPath(d => d.Entry.IsFavorite, s => s.MapFrom(m => m.Entry.IsFavorite))
                .ForPath(d => d.Entry.IsFile, s => s.MapFrom(m => m.Entry.IsFile))
                .ForPath(d => d.Entry.IsFolder, s => s.MapFrom(m => m.Entry.IsFolder))
                .ForPath(d => d.Entry.IsLink, s => s.MapFrom(m => m.Entry.IsLink))
                .ForPath(d => d.Entry.IsLocked, s => s.MapFrom(m => m.Entry.IsLocked))
                .ForPath(d => d.Entry.ModifiedAt, s => s.MapFrom(m => m.Entry.ModifiedAt))
                .ForPath(d => d.Entry.NodeType, s => s.MapFrom(m => m.Entry.NodeType))
                .ForPath(d => d.Entry.ParentId, s => s.MapFrom(m => m.Entry.ParentId))
                .ForPath(d => d.Entry.CreatedByUser, s => s.MapFrom(m => m.Entry.CreatedByUser))
                .ForPath(d => d.Entry.ModifiedByUser, s => s.MapFrom(m => m.Entry.ModifiedByUser));

            CreateMap<NodeAssociationEntry, NodeEntry>()
                .ForPath(d => d.Entry.Id, s => s.MapFrom(m => m.Entry.Id))
                .ForPath(d => d.Entry.Name, s => s.MapFrom(m => m.Entry.Name))
                .ForPath(d => d.Entry.Path, s => s.MapFrom(m => m.Entry.Path))
                .ForPath(d => d.Entry.Permissions, s => s.MapFrom(m => m.Entry.Permissions))
                .ForPath(d => d.Entry.Content, s => s.MapFrom(m => m.Entry.Content))
                .ForPath(d => d.Entry.Properties, s => s.MapFrom(m => m.Entry.Properties))
                .ForPath(d => d.Entry.AllowableOperations, s => s.MapFrom(m => m.Entry.AllowableOperations))
                .ForPath(d => d.Entry.AspectNames, s => s.MapFrom(m => m.Entry.AspectNames))
                .ForPath(d => d.Entry.CreatedAt, s => s.MapFrom(m => m.Entry.CreatedAt))
                .ForPath(d => d.Entry.IsFavorite, s => s.MapFrom(m => m.Entry.IsFavorite))
                .ForPath(d => d.Entry.IsFile, s => s.MapFrom(m => m.Entry.IsFile))
                .ForPath(d => d.Entry.IsFolder, s => s.MapFrom(m => m.Entry.IsFolder))
                .ForPath(d => d.Entry.IsLink, s => s.MapFrom(m => m.Entry.IsLink))
                .ForPath(d => d.Entry.IsLocked, s => s.MapFrom(m => m.Entry.IsLocked))
                .ForPath(d => d.Entry.ModifiedAt, s => s.MapFrom(m => m.Entry.ModifiedAt))
                .ForPath(d => d.Entry.NodeType, s => s.MapFrom(m => m.Entry.NodeType))
                .ForPath(d => d.Entry.ParentId, s => s.MapFrom(m => m.Entry.ParentId))
                .ForPath(d => d.Entry.CreatedByUser, s => s.MapFrom(m => m.Entry.CreatedByUser))
                .ForPath(d => d.Entry.ModifiedByUser, s => s.MapFrom(m => m.Entry.ModifiedByUser));
        }

        #endregion
    }
}
