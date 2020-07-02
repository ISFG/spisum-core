using System.Threading.Tasks;

namespace ISFG.Translations.Infrastructure
{
    public interface ITranslateService
    {
        #region Public Methods

        Task<string> Translate(string key);
        Task<string> Translate(string key, string nodeType, string translationType);

        #endregion
    }
}