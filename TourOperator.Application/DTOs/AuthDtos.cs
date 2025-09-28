namespace TourOperator.Application.DTOs
{
    public record RegisterRequest(string Email, string Password, string Role, Guid? TourOperatorId);
    public record AuthResponse(string Token, DateTime ExpiresAt);
    public record LoginRequest(string Email, string Password);
}
