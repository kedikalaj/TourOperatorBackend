using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using TourOperator.Application.DTOs;
using TourOperator.Application.Interfaces;
using TourOperator.Domain.Entities;
using TourOperator.Infrastructure.Data;

namespace TourOperator.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IDatabase _redis;

        public AuthService(AppDbContext db, IJwtTokenService jwtTokenService, IConnectionMultiplexer mux)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
            _redis = mux.GetDatabase();
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email already exists");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                TourOperatorId = request.TourOperatorId
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Log.Information("User registered {Email} role={Role}", user.Email, user.Role);

            var token = _jwtTokenService.GenerateToken(user);
            return new AuthResponse(token, DateTime.UtcNow.AddHours(6));
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            var token = _jwtTokenService.GenerateToken(user);
            Log.Information("User logged in {Email}", user.Email);

            return new AuthResponse(token, DateTime.UtcNow.AddHours(6));
        }

        public async Task LogoutAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti)) return;

            var expUnix = long.Parse(jwt.Claims.First(c => c.Type == "exp").Value);
            var exp = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            var ttl = exp - DateTimeOffset.UtcNow;

            if (ttl.TotalSeconds > 0)
                await _redis.StringSetAsync($"bl_jti:{jti}", "1", ttl);

            Log.Information("Token jti {Jti} blacklisted until {Exp}", jti, exp);
        }
    }
}
