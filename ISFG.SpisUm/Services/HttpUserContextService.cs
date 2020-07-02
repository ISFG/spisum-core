using AutoMapper;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Identity;
using Microsoft.AspNetCore.Http;

namespace ISFG.SpisUm.Services
{
    public class HttpUserContextService : IHttpUserContextService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccess;
        private readonly IMapper _mapper;

        #endregion

        #region Constructors

        public HttpUserContextService(IHttpContextAccessor httpContextAccess, IMapper mapper)
        {
            _httpContextAccess = httpContextAccess;
            _mapper = mapper;
        }

        #endregion

        #region Implementation of IHttpUserContextService

        public IIdentityUser Current => _mapper.Map<IdentityUser>(_httpContextAccess.HttpContext);

        #endregion
    }
}