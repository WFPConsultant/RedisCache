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

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] int? categoryId, [FromQuery] decimal? maxPrice)
        {
            var products = await _productService.GetProductsByFilterAsync(categoryId, maxPrice);
            return Ok(products);
        }

        [HttpDelete("{id}/cache")]
        public IActionResult InvalidateCache(int id)
        {
            var userId = Request.Headers["UserId"];
            var cacheingKey = $"ProductWithCategory_{id}_{userId}";

            _cache.RemoveData(cacheingKey);

            return NoContent(); 
        }
    }
}
