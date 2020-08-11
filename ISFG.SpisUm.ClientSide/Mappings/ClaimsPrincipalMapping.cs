using System.Linq;
using System.Security.Claims;
using AutoMapper;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.SpisUm.ClientSide.Converters;
using ISFG.SpisUm.ClientSide.Identity;
using ISFG.SpisUm.ClientSide.Models;
using Microsoft.AspNetCore.Http;

namespace ISFG.SpisUm.ClientSide.Mappings
{
    public class ClaimsPrincipalMapping : Profile
    {
        #region Fields

        public static readonly string Token = @"http://spisum.cz/identity/claims/token";
        public static readonly string Group = @"http://spisum.cz/identity/claims/group";
        public static readonly string OrganizationUserId = @"http://spisum.cz/identity/claims/organizationUserId";
        public static readonly string OrganizationId = @"http://spisum.cz/identity/claims/organizationId";
        public static readonly string OrganizationName = @"http://spisum.cz/identity/claims/organizationName";
        public static readonly string OrganizationUnit = @"http://spisum.cz/identity/claims/organizationUnit";
        public static readonly string OrganizationAddress = @"http://spisum.cz/identity/claims/organizationAddress";
        public static readonly string Job = @"http://spisum.cz/identity/claims/job";

        #endregion

        #region Constructors

        public ClaimsPrincipalMapping()
        {
            CreateMap<(PersonEntryFixed User, string Token), ClaimsPrincipal>().ConvertUsing<ClaimsPrincipalConverter>();

            CreateMap<HttpContext, IdentityUser>()
                .ForMember(d => d.Id, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name).Value))
                .ForMember(d => d.FirstName, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName).Value))
                .ForMember(d => d.LastName, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname).Value))
                .ForMember(d => d.Token, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == Token).Value))
                .ForMember(d => d.Group, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == Group).Value))
                .ForMember(d => d.OrganizationUserId, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == OrganizationUserId).Value))
                .ForMember(d => d.OrganizationId, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == OrganizationId).Value))
                .ForMember(d => d.OrganizationName, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == OrganizationName).Value))
                .ForMember(d => d.OrganizationUnit, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == OrganizationUnit).Value))
                .ForMember(d => d.OrganizationAddress, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == OrganizationAddress).Value))
                .ForMember(d => d.Job, s => s.MapFrom(m => m.User.Claims.FirstOrDefault(x => x.Type == Job).Value))
                .ForMember(d => d.IsAdmin, s => s.MapFrom(m => m.User.Identity.AuthenticationType == AlfrescoIdentityTypes.AlfrescoAdminIdentity))
                .ForMember(d => d.RequestGroup, s => s.MapFrom(x => GetGroupFromHeader(x)));
        }

        #endregion

        #region Static Methods

        private static string GetGroupFromHeader(HttpContext httpContext)
        {
            string group = httpContext?.Request?.Headers[SpisumNames.Headers.Group];
            
            if (string.IsNullOrWhiteSpace(group))
                group = httpContext?.Request?.Query["requestGroup"];
            
            return group ?? string.Empty;
        }

        #endregion
    }
}