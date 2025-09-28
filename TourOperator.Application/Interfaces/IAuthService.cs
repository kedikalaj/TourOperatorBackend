using TourOperator.Application.DTOs;

namespace TourOperator.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task LogoutAsync(string token);
    }
}
