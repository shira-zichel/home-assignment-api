using HomeAssignment.Models;
using HomeAssignment.Repositories;
using HomeAssignment.Configuration;
using Microsoft.Extensions.Options;

namespace HomeAssignment.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<User?> RegisterAsync(RegisterRequest request);
        Task<User?> GetUserByIdAsync(int id);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by username
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed - user not found: {Username}", request.Username);
                    return null;
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed - invalid password for user: {Username}", request.Username);
                    return null;
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

                _logger.LogInformation("User logged in successfully: {Username} with role: {Role}",
                    user.Username, user.Role);

                return new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return null;
            }
        }

        public async Task<User?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                if (await _userRepository.ExistsAsync(request.Username))
                {
                    _logger.LogWarning("Registration failed - username already exists: {Username}", request.Username);
                    return null;
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create user
                var user = new User
                {
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    Role = request.Role
                };

                var createdUser = await _userRepository.CreateAsync(user);

                _logger.LogInformation("User registered successfully: {Username} with role: {Role}",
                    createdUser.Username, createdUser.Role);

                // Don't return password hash
                createdUser.PasswordHash = string.Empty;
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null)
            {
                // Don't return password hash
                user.PasswordHash = string.Empty;
            }
            return user;
        }
    }
}
