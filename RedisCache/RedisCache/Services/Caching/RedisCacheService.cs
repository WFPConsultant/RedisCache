using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RedisCache.Services.Caching
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache? _cache;

        public RedisCacheService(IDistributedCache? cache)
        {
            _cache = cache;
        }
        public T? GetData<T>(string key)
        {
            var data = _cache?.GetString(key);
            if (data == null)
            {
                return default(T);
            }
            return JsonSerializer.Deserialize<T>(data);
        }       

        public void SetData<T>(string key, T data)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Set expiration time as needed
                // There are more settings where if we want to implement here then we can write them here
                
            };
            _cache?.SetString(key, JsonSerializer.Serialize(data), options);
        }
        public void RemoveData(string key)
        {
            _cache?.Remove(key);
        }
    }
}
