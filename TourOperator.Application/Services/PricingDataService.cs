using Microsoft.EntityFrameworkCore;
using TourOperator.Application.DTOs;
using TourOperator.Application.Interfaces;
using TourOperator.Infrastructure.Data;

namespace TourOperator.Application.Services
{
    public class PricingDataService : IPricingDataService
    {
        private readonly AppDbContext _context;

        public PricingDataService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PricingDataDto>> GetPricingDataAsync(Guid tourOperatorId, int page, int pageSize)
        {
            return await _context.PricingRecords
                .Where(p => p.TourOperatorId == tourOperatorId)
                .OrderBy(p => p.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PricingDataDto
                {
                    RouteCode = p.RouteCode,
                    SeasonCode = p.SeasonCode,
                    Date = p.Date,
                    EconomySeats = p.EconomySeats,
                    BusinessSeats = p.BusinessSeats,
                    EconomyPrice = p.EconomyPrice,
                    BusinessPrice = p.BusinessPrice
                })
                .ToListAsync();
        }
    }
}
