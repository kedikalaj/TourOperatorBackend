using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using TourOperator.Application.Interfaces;

namespace TourOperator.Controllers
{
    [ApiController]
    [Route("api/data/{tourOperatorId:guid}")]
    public class AdminController : ControllerBase
    {
        private readonly IPricingDataService _pricingDataService;
        private readonly IDistributedCache _cache;

        public AdminController(IPricingDataService pricingDataService, IDistributedCache cache)
        {
            _pricingDataService = pricingDataService;
            _cache = cache;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPricing([FromRoute] Guid tourOperatorId, int page = 1, int pageSize = 50)
        {
            // Generate a cache key based on tourOperatorId, page, and pageSize to ensure unique caching per query
            var cacheKey = $"pricing_data_{tourOperatorId}_{page}_{pageSize}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                // if cached data exists, return it directly
                Log.Information("Cache hit for pricing data for tourOperatorId={TourOperatorId}", tourOperatorId);
                return Ok(cachedData);
            }

            // otherwise, fetch data from the database
            var pricingData = await _pricingDataService.GetPricingDataAsync(tourOperatorId, page, pageSize);

            if (pricingData != null)
            {
                await _cache.SetStringAsync(cacheKey, pricingData.ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache for 30 minutes
                });
            }

            Log.Information("Cache miss for pricing data for tourOperatorId={TourOperatorId}", tourOperatorId);
            return Ok(pricingData);
        }
    }
}
