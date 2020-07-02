using Microsoft.Extensions.Caching.Memory;

namespace ISFG.Common.Interfaces
{
    public interface ISimpleMemoryCache
    {
        #region Public Methods

        void Create<T>(string key, T data, MemoryCacheEntryOptions memoryCacheEntryOptions = null);
        void Delete(string key);
        T Get<T>(string key);
        bool IsExist(string key);

        #endregion
    }
}