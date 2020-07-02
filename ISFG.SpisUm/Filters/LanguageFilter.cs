using System.Globalization;
using ISFG.SpisUm.Dictionaries;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace ISFG.SpisUm.Filters
{
    public class LanguageFilter : IActionFilter
    {
        #region Implementation of IActionFilter

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var language = Languages.GetLanguageFromAvailableLanguages(context?.HttpContext?.Request?.Headers[HeaderNames.AcceptLanguage]);
            CultureInfo.CurrentCulture = new CultureInfo(language.Code);
        }

        #endregion
    }
}