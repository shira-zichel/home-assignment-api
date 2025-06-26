using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeAssignment.Configuration;
using HomeAssignment.Models;

namespace HomeAssignment.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;

            // Debug: Validate settings
            if (string.IsNullOrEmpty(_jwtSettings.SecretKey) || _jwtSettings.SecretKey.Length < 32)
            {
                throw new ArgumentException("JWT SecretKey must be at least 32 characters long");
            }

            _logger.LogInformation("JwtService initialized with Issuer: {Issuer}, Audience: {Audience}",
                _jwtSettings.Issuer, _jwtSettings.Audience);
        }

        public string GenerateToken(User user)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for user: {Username} with role: {Role}",
                    user.Username, user.Role);

                // Create signing key
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("username", user.Username),
                    new Claim("role", user.Role.ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                };

                // Create token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = credentials
                };

                // Generate token
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(securityToken);

                _logger.LogInformation("JWT token generated successfully. Token length: {Length}", tokenString.Length);

                // Debug: Log first/last 10 characters of token
                if (tokenString.Length > 20)
                {
                    _logger.LogDebug("Token preview: {Start}...{End}",
                        tokenString.Substring(0, 10),
                        tokenString.Substring(tokenString.Length - 10));
                }

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {Username}", user.Username);
                throw;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Token validation failed: Token is null or empty");
                    return null;
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                _logger.LogInformation("Token validated successfully for user: {Username}",
                    principal.FindFirst(ClaimTypes.Name)?.Value);

                return principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token validation failed: Token expired - {Message}", ex.Message);
                return null;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("Token validation failed: Invalid signature - {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
                return null;
            }
        }
    }
}