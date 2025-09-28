using TourOperator.Application.DTOs;

namespace TourOperator.Application.Interfaces
{
    public interface IPricingDataService
    {
        public Task<IEnumerable<PricingDataDto>> GetPricingDataAsync(Guid tourOperatorId, int page, int pageSize);
    }
}
