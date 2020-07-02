using System;
using ISFG.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class SimpleMemoryCache : ISimpleMemoryCache
    {
        #region Fields

        private readonly MemoryCacheEntryOptions _cacheEntryOptions;
        private readonly MemoryCache _memoryCache;

        private readonly object _sync = new object();

        #endregion

        #region Constructors

        public SimpleMemoryCache()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                SlidingExpiration = TimeSpan.FromHours(12)
            };
        }

        #endregion

        #region Implementation of ISimpleMemoryCache

        public void Create<T>(string key, T data, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
        {
            lock (_sync)
            {
                if (IsExist(key))
                    throw new Exception($"{nameof(SimpleMemoryCache)} key {key} already exists.");

                _memoryCache.Set(key, data, memoryCacheEntryOptions ?? _cacheEntryOptions);
            }
        }

        public void Delete(string key)
        {
            lock (_sync)
            {
                if (_memoryCache.Get(key) != null)
                    _memoryCache.Remove(key);
            }
        }

        public T Get<T>(string key)
        {
            lock (_sync)
            {
                return _memoryCache.Get<T>(key);
            }
        }

        public bool IsExist(string key)
        {
            lock (_sync)
            {
                return _memoryCache.TryGetValue(key, out _);
            }
        }

        #endregion
    }
}