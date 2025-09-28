using TourOperator.Domain.Entities;

namespace TourOperator.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
