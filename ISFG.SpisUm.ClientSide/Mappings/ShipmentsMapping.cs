using AutoMapper;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using ISFG.SpisUm.ClientSide.ServiceModels.Shipments;

namespace ISFG.SpisUm.ClientSide.Mappings
{
    public class ShipmentsMapping : Profile
    {
        #region Constructors

        public ShipmentsMapping()
        {
            CreateMap<ShipmentCreateEmail, ShipmentCreateEmailSModel>()
                .ForMember(d => d.NodeId, s => s.MapFrom(m => m.NodeId))
                .ForMember(d => d.Recipient, s => s.MapFrom(m => m.Body.Recipient))
                .ForMember(d => d.Sender, s => s.MapFrom(m => m.Body.Sender))
                .ForMember(d => d.Subject, s => s.MapFrom(m => m.Body.Subject))
                .ForMember(d => d.Components, s => s.MapFrom(m => m.Body.Components));

            CreateMap<ShipmentCreateDataBox, ShipmentCreateDataBoxSModel>()
                .ForMember(d => d.NodeId, s => s.MapFrom(m => m.NodeId))
                .ForMember(d => d.Recipient, s => s.MapFrom(m => m.Body.Recipient))
                .ForMember(d => d.Sender, s => s.MapFrom(m => m.Body.Sender))
                .ForMember(d => d.Subject, s => s.MapFrom(m => m.Body.Subject))
                .ForMember(d => d.Components, s => s.MapFrom(m => m.Body.Components))
                .ForMember(d => d.AllowSubstDelivery, s => s.MapFrom(m => m.Body.AllowSubstDelivery))
                .ForMember(d => d.LegalTitleLaw, s => s.MapFrom(m => m.Body.LegalTitleLaw))
                .ForMember(d => d.LegalTitlePar, s => s.MapFrom(m => m.Body.LegalTitlePar))
                .ForMember(d => d.LegalTitlePoint, s => s.MapFrom(m => m.Body.LegalTitlePoint))
                .ForMember(d => d.LegalTitleSect, s => s.MapFrom(m => m.Body.LegalTitleSect))
                .ForMember(d => d.LegalTitleYear, s => s.MapFrom(m => m.Body.LegalTitleYear))
                .ForMember(d => d.PersonalDelivery, s => s.MapFrom(m => m.Body.PersonalDelivery))
                .ForMember(d => d.ToHands, s => s.MapFrom(m => m.Body.ToHands));

            CreateMap<ShipmentCreatePersonally, ShipmentCreatePersonallySModel>()
                .ForMember(d => d.NodeId, s => s.MapFrom(m => m.NodeId))
                .ForMember(d => d.Address1, s => s.MapFrom(m => m.Body.Address1))
                .ForMember(d => d.Address2, s => s.MapFrom(m => m.Body.Address2))
                .ForMember(d => d.Address3, s => s.MapFrom(m => m.Body.Address3))
                .ForMember(d => d.Address4, s => s.MapFrom(m => m.Body.Address4))
                .ForMember(d => d.AddressCity, s => s.MapFrom(m => m.Body.AddressCity))
                .ForMember(d => d.AddressState, s => s.MapFrom(m => m.Body.AddressState))
                .ForMember(d => d.AddressStreet, s => s.MapFrom(m => m.Body.AddressStreet))
                .ForMember(d => d.AddressZip, s => s.MapFrom(m => m.Body.AddressZip));

            CreateMap<ShipmentCreatePost, ShipmentCreatePostSModel>()
                .ForMember(d => d.NodeId, s => s.MapFrom(m => m.NodeId))
                .ForMember(d => d.Address1, s => s.MapFrom(m => m.Body.Address1))
                .ForMember(d => d.Address2, s => s.MapFrom(m => m.Body.Address2))
                .ForMember(d => d.Address3, s => s.MapFrom(m => m.Body.Address3))
                .ForMember(d => d.Address4, s => s.MapFrom(m => m.Body.Address4))
                .ForMember(d => d.AddressCity, s => s.MapFrom(m => m.Body.AddressCity))
                .ForMember(d => d.AddressState, s => s.MapFrom(m => m.Body.AddressState))
                .ForMember(d => d.AddressStreet, s => s.MapFrom(m => m.Body.AddressStreet))
                .ForMember(d => d.AddressZip, s => s.MapFrom(m => m.Body.AddressZip))
                .ForMember(d => d.PostItemType, s => s.MapFrom(m => m.Body.PostItemType))
                .ForMember(d => d.PostItemTypeOther, s => s.MapFrom(m => m.Body.PostItemTypeOther))
                .ForMember(d => d.PostItemStatedPrice, s => s.MapFrom(m => m.Body.PostItemStatedPrice))
                .ForMember(d => d.PostItemCashOnDelivery, s => s.MapFrom(m => m.Body.PostItemCashOnDelivery))
                .ForMember(d => d.PostType, s => s.MapFrom(m => m.Body.PostType))
                .ForMember(d => d.PostTypeOther, s => s.MapFrom(m => m.Body.PostTypeOther));
        }

        #endregion
    }
}