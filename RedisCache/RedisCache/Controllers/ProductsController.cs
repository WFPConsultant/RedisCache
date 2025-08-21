using Microsoft.AspNetCore.Mvc;
using RedisCache.Models;
using RedisCache.Services;
using RedisCache.Services.Caching;

namespace RedisCache.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly IRedisCacheService _cache;
        public ProductsController(ProductService productService, IRedisCacheService cache)
        {
            _productService = productService;
            _cache = cache;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var userId = Request.Headers["UserId"]; //if we want to use user-specific cache. If we have claims information then use it here.
            var cacheingKey = $"ProductWithCategory_{userId}";

            var products = _cache.GetData<IEnumerable<ProductDto>>(cacheingKey);
            if (products != null)
                return Ok(products);
            products = await _productService.GetAllProducts();

            _cache.SetData(cacheingKey, products);
            return products != null ? Ok(products) : NotFound();
        }
        /// <summary>
        /// Gets a single product by its ID.
        /// </summary>
        /// <remarks>
        /// Try calling this multiple times. The first call will be slower (cache miss),
        /// subsequent calls within 5 minutes will be very fast (cache hit).
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var userId = Request.Headers["UserId"];
            var cacheKey = $"ProductWithCategory_{id}_{userId}";

            var product = _cache.GetData<ProductDto>(cacheKey);
            if (product != null)
                return Ok(product);

            product = await _productService.GetProductByIdAsync(id, userId);
            if (product != null)
                _cache.SetData(cacheKey, product);

            return product != null ? Ok(product) : NotFound();
        }

        /// <summary>
        /// Searches for products using optional filters.
        /// </summary>
        /// <remarks>
        /// Example URLs:
        /// /api/products/search
        /// /api/products/search?categoryId=2
        /// /api/products/search?categoryId=1&maxPrice=100
        /// Each unique combination of parameters will have its own cache entry.
        /// </remarks>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] int? categoryId, [FromQuery] decimal? maxPrice)
        {
            var products = await _productService.GetProductsByFilterAsync(categoryId, maxPrice);
            return Ok(products);
        }

        /// <summary>
        /// Invalidates the cache for a single product.
        /// </summary>
        /// <remarks>
        /// Call this endpoint after you update or delete a product in the database.
        /// The next time GET /api/products/{id} is called, it will fetch fresh data.
        /// </remarks>
        [HttpDelete("{id}/cache")]
        public IActionResult InvalidateCache(int id)
        {
            var userId = Request.Headers["UserId"];
            var cacheingKey = $"ProductWithCategory_{id}_{userId}";

            _cache.RemoveData(cacheingKey); // Use the new method

            return NoContent(); // 204 No Content is a standard response for a successful delete/invalidation.
        }
    }
}
