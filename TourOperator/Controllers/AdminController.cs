using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using TourOperator.Application.Interfaces;

namespace TourOperator.Controllers
{
    [ApiController]
    [Route("api/data/{tourOperatorId:guid}")]
    public class AdminController : ControllerBase
    {
        private readonly IPricingDataService _pricingDataService;
        private readonly IDistributedCache _cache;
        private readonly IDatabase _redis;

        public AdminController(IPricingDataService pricingDataService, IDistributedCache cache, IConnectionMultiplexer mux)
        {
            _pricingDataService = pricingDataService;
            _cache = cache;
            _redis = mux.GetDatabase();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPricing([FromRoute] Guid tourOperatorId, int page = 1, int pageSize = 50)
        {

            // this is an examle of how the blacklisted token will be checked
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                var isBlacklisted = await _redis.KeyExistsAsync($"bl_jti:{jti}");
                if (isBlacklisted)
                {
                    return Unauthorized("Token has been blacklisted.");
                }
            }

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
