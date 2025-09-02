using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;

namespace Frontend.Services
{
    public class CacheService
    {
        private readonly MemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(ILogger<CacheService> logger)
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000 // Límite de entradas en cache
            });
            _logger = logger;
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug($"Cache HIT para: {key}");
                return cachedValue;
            }

            _logger.LogDebug($"Cache MISS para: {key}");
            var value = await factory();
            
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(5),
                Size = 1,
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(key, value, options);
            return value;
        }

        public void Remove(string key) 
        {
            _cache.Remove(key);
            _logger.LogDebug($"Cache removido: {key}");
        }
        
        public void RemoveByPattern(string pattern)
        {
            try
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    
                if (field?.GetValue(_cache) is IDictionary dict)
                {
                    var keysToRemove = new List<object>();
                    
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (entry.Key.ToString()?.Contains(pattern) == true)
                            keysToRemove.Add(entry.Key);
                    }
                    
                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                        _logger.LogDebug($"Cache removido por patrón '{pattern}': {key}");
                    }
                    
                    if (keysToRemove.Any())
                    {
                        _logger.LogInformation($"Invalidadas {keysToRemove.Count} entradas de cache con patrón: {pattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removiendo cache por patrón: {pattern}");
            }
        }
        
        public void Clear() 
        {
            _cache.Clear();
            _logger.LogInformation("Cache completamente limpiado");
        }
    }
}