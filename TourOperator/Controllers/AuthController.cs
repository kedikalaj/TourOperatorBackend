using Microsoft.AspNetCore.Mvc;
using TourOperator.Application.DTOs;
using TourOperator.Application.Interfaces;

namespace TourOperator.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                var response = await _authService.RegisterAsync(req);
                return Ok(response);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try
            {
                var response = await _authService.LoginAsync(req);
                return Ok(response);
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _authService.LogoutAsync(token);
            return Ok();
        }
    }
}