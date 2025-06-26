using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HomeAssignment.Models;
using HomeAssignment.Services;
using System.Security.Claims;

namespace HomeAssignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login with username and password to get JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user info</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            return Ok(result);
        }

        

        /// <summary>
        /// Get current authenticated user info
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            });
        }

        /// <summary>
        /// Get available test credentials
        /// </summary>
        /// <returns>Test user credentials for development</returns>
        [HttpGet("test-credentials")]
        public IActionResult GetTestCredentials()
        {
            return Ok(new
            {
                Message = "Use these credentials to test authentication",
                TestUsers = new[]
                {
                    new { Username = "admin", Password = "admin123", Role = "Admin", Permissions = "Full access - can create, read, update, delete" },
                    new { Username = "user", Password = "user123", Role = "User", Permissions = "Read-only access - can only fetch data" }
                },
                Instructions = new[]
                {
                    "1. POST /api/auth/login with username and password",
                    "2. Copy the 'token' from the response",
                    "3. Add header: Authorization: Bearer {token}",
                    "4. Admin can use all endpoints, User can only GET"
                }
            });
        }
    }
}