using RedisCache.Data;
using RedisCache.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace RedisCache.Services
{
    public class ProductService
    {
        private readonly IMemoryCache _cache;
        private readonly ProductRepository _repository;
        private readonly ILogger<ProductService> _logger;

        // Standard cache options for our entries
        private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)) // Evict if not accessed in 5 mins
                .SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Evict after 1 hour, regardless of activity

        public ProductService(IMemoryCache cache, ProductRepository repository, ILogger<ProductService> logger)
        {
            _cache = cache;
            _repository = repository;
            _logger = logger;
        }
        public async Task<IEnumerable<ProductDto>> GetAllProducts()
        {
            var productsFromRepo = await _repository.GetAllProducts();
            return productsFromRepo;
        }
        /// <summary>
        /// Gets a single product, using the cache first.
        /// </summary>
        public async Task<ProductDto?> GetProductByIdAsync(int productId, string? userId = null)
        {
            var productFromRepo = await _repository.GetProductByIdAsync(productId);
            return productFromRepo;
        }

        /// <summary>
        /// Gets a list of products based on filters, using the cache first.
        /// </summary>
        public async Task<IEnumerable<ProductDto>> GetProductsByFilterAsync(int? categoryId, decimal? maxPrice)
        {
            var cacheKey = GenerateFilterCacheKey(categoryId, maxPrice);

            // Try to get the list from the cache first
            if (_cache.TryGetValue(cacheKey, out IEnumerable<ProductDto>? cachedProducts) && cachedProducts != null)
            {
                _logger.LogInformation("Cache HIT for key: {CacheKey}", cacheKey);
                return cachedProducts;
            }

            // If cache miss, fetch from the repository
            _logger.LogWarning("Cache MISS for key: {CacheKey}. Fetching from repository.", cacheKey);
            var productsFromRepo = await _repository.GetProductsByFilterAsync(categoryId, maxPrice);

            // Add the result to the cache
            _cache.Set(cacheKey, productsFromRepo, _cacheOptions);

            return productsFromRepo;
        }      
        /// <summary>
        /// Helper method to create a consistent cache key from filter parameters.
        /// </summary>
        private string GenerateFilterCacheKey(int? categoryId, decimal? maxPrice)
        {
            var keyBuilder = new StringBuilder("products-filter");
            keyBuilder.Append($"-cat:{categoryId?.ToString() ?? "any"}");
            keyBuilder.Append($"-price:{maxPrice?.ToString() ?? "any"}");
            return keyBuilder.ToString();
        }
    }
}
