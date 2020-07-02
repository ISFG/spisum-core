using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;

namespace ISFG.SpisUm.ClientSide.Mappings
{
    public class AuthorizationMapping : Profile
    {
        #region Constructors

        public AuthorizationMapping()
        {
            CreateMap<(TicketEntry a, PersonEntryFixed p, IAlfrescoConfiguration c, ISignerConfiguration s), Authorization>()
                .ForMember(d => d.User, s => s.MapFrom(m => m.a.Entry.UserId))
                .ForMember(d => d.Token, s => s.MapFrom(m => m.a.Entry.Id))
                .ForMember(d => d.AuthorizationToken, s => s.MapFrom(m => m.a.Entry.Id.ToAlfrescoAuthentication()))
                .ForMember(d => d.Expire, s => s.MapFrom(m => m.c.TokenExpire))
                .ForMember(d => d.Signer, s => s.MapFrom(m => IsSigner(m.s)))
                .ForMember(d => d.IsAdmin, s => s.MapFrom(m => IsAdmin(m.p)));
        }

        #endregion

        #region Static Methods

        private static bool IsAdmin(PersonEntryFixed personEntry)
        {
            var capabilities = personEntry.Entry?.Capabilities?.As<JObject>()?.ToDictionary();
            var isAdmin = capabilities?.GetNestedValueOrDefault(AlfrescoNames.Capabilities.IsAdmin)?.ToString();
            
            try
            {
                return bool.Parse(isAdmin);
            }
            catch
            {
                return false;
            }
        }
        
        private static bool IsSigner(ISignerConfiguration signerConfiguration) => 
            signerConfiguration?.Base?.Uri != null;

        #endregion
    }
}