using System.Threading.Tasks;
using ISFG.SpisUm.Endpoints;
using ISFG.Translations.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/translations")]
    public class TranslationsController : ControllerBase
    {
        #region Fields

        private readonly ITranslateService _translateService;

        #endregion

        #region Constructors

        public TranslationsController(ITranslateService translateService)
        {
            _translateService = translateService;
        }

        #endregion

        #region Public Methods

        [HttpGet("translate")]
        public async Task<string> GetTransactionHistory([FromQuery] string key)
        {
            return await _translateService.Translate(key);
        }

        #endregion
    }
}