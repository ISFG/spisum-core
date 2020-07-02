using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.SpisUm.ClientSide.Mappings;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;

namespace ISFG.SpisUm.ClientSide.Converters
{
    public class ClaimsPrincipalConverter : ITypeConverter<(PersonEntryFixed User, string Token), ClaimsPrincipal>
    {
        #region Implementation of ITypeConverter<(PersonEntryFixed User, string Token),ClaimsPrincipal>

        public ClaimsPrincipal Convert((PersonEntryFixed User, string Token) source, ClaimsPrincipal destination, ResolutionContext context)
        {
            (PersonEntryFixed user, string token) = source;
            var capabilities = user.Entry?.Capabilities?.As<JObject>()?.ToDictionary();
            var isAdmin = capabilities?.GetNestedValueOrDefault(AlfrescoNames.Capabilities.IsAdmin)?.ToString();

            ClaimsIdentity identity;
            if (!string.IsNullOrEmpty(isAdmin) && isAdmin.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
                identity = new ClaimsIdentity(AlfrescoIdentityTypes.AlfrescoAdminIdentity);
            else
                identity = new ClaimsIdentity(AlfrescoIdentityTypes.AlfrescoIdentity);

            identity.AddClaim(new Claim(ClaimTypes.Name, user?.Entry?.Id));
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user?.Entry?.FirstName ?? string.Empty));
            identity.AddClaim(new Claim(ClaimTypes.Surname, user?.Entry?.LastName ?? string.Empty));
            identity.AddClaim(new Claim(ClaimsPrincipalMapping.Token, token));

            var properties = user.Entry?.Properties?.As<JObject>()?.ToDictionary();
            if (properties == null)
                return new ClaimsPrincipal(identity);

            List<Claim> propertiesClaims = new List<Claim>();
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.Group, properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.OrganizationUserId, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserId)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.OrganizationId, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgId)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.OrganizationName, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgName)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.OrganizationUnit, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgUnit)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.OrganizationAddress, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgAddress)?.ToString()));
            propertiesClaims.Add(CreateClaim(ClaimsPrincipalMapping.Job, properties.GetNestedValueOrDefault(SpisumNames.Properties.UserJob)?.ToString()));

            identity.AddClaims(propertiesClaims.Where(x => x != null));

            return new ClaimsPrincipal(identity);
        }

        #endregion

        #region Static Methods

        private static Claim CreateClaim(string type, string value)
        {
            if (type != null && value != null)
                return new Claim(type, value);

            return null;
        }

        #endregion
    }
}