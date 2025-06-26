using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using HomeAssignment.Services;
using HomeAssignment.Configuration;
using HomeAssignment.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HomeAssignment.Tests.Services
{
    public class JwtServiceTests
    {
        private readonly Mock<ILogger<JwtService>> _mockLogger;
        private readonly JwtSettings _jwtSettings;
        private readonly JwtService _jwtService;

        public JwtServiceTests()
        {
            _mockLogger = new Mock<ILogger<JwtService>>();
            _jwtSettings = new JwtSettings
            {
                SecretKey = "MyVeryLongAndSecureSecretKeyForJWTTokenGeneration123456789",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            };

            var mockOptions = new Mock<IOptions<JwtSettings>>();
            mockOptions.Setup(x => x.Value).Returns(_jwtSettings);

            _jwtService = new JwtService(mockOptions.Object, _mockLogger.Object);
        }

        [Fact]
        public void GenerateToken_WithValidUser_ShouldReturnValidJwtToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Role = UserRole.User
            };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3); // JWT has 3 parts

            // Verify token can be parsed
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            jsonToken.Should().NotBeNull();
            jsonToken.Issuer.Should().Be(_jwtSettings.Issuer);
            jsonToken.Audiences.Should().Contain(_jwtSettings.Audience);
        }

        [Fact]
        public void GenerateToken_WithAdminUser_ShouldContainCorrectClaims()
        {
            // Arrange
            var user = new User
            {
                Id = 42,
                Username = "admin",
                Role = UserRole.Admin
            };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var claims = jsonToken.Claims.ToList();

            // Debug: Print all claims to see what we actually get
            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }

            // Use the actual claim type names from JWT
            claims.Should().Contain(c => c.Type == "nameid" && c.Value == "42");
            claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "admin");
            claims.Should().Contain(c => c.Type == "role" && c.Value == user.Role.ToString());
            claims.Should().Contain(c => c.Type == "username" && c.Value == "admin");
            claims.Should().Contain(c => c.Type == "sub" && c.Value == "42");
        }

        [Fact]
        public void GenerateToken_WithUserRole_ShouldContainCorrectClaims()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "regularuser",
                Role = UserRole.User
            };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var claims = jsonToken.Claims.ToList();

            // Use the actual claim type names from JWT
            claims.Should().Contain(c => c.Type == "nameid" && c.Value == "1");
            claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "regularuser");
            claims.Should().Contain(c => c.Type == "role" && c.Value == user.Role.ToString());
            claims.Should().Contain(c => c.Type == "username" && c.Value == "regularuser");
            claims.Should().Contain(c => c.Type == "sub" && c.Value == "1");
        }

        [Fact]
        public void GenerateToken_ShouldSetCorrectExpiration()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Role = UserRole.User };
            var beforeGeneration = DateTime.UtcNow;

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.ExpirationMinutes);
            jsonToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GenerateToken_ShouldIncludeJtiClaim()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Role = UserRole.User };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var jtiClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "jti");
            jtiClaim.Should().NotBeNull();
            Guid.TryParse(jtiClaim.Value, out _).Should().BeTrue();
        }

        [Fact]
        public void GenerateToken_ShouldIncludeTimestampClaims()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Role = UserRole.User };
            var beforeGeneration = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var iatClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "iat");
            var nbfClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "nbf");
            var expClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "exp");

            iatClaim.Should().NotBeNull();
            nbfClaim.Should().NotBeNull();
            expClaim.Should().NotBeNull();

            // Verify timestamp values are reasonable
            var iatValue = long.Parse(iatClaim.Value);
            var expValue = long.Parse(expClaim.Value);

            iatValue.Should().BeGreaterThanOrEqualTo(beforeGeneration);
            expValue.Should().BeGreaterThan(iatValue);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser", Role = UserRole.User };
            var token = _jwtService.GenerateToken(user);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();

            // When validating, the claims should be mapped back to the standard ClaimTypes
            // because of the RoleClaimType and NameClaimType configuration in your Program.cs
            principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be("testuser");
            principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(user.Role.ToString());
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("1");
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var principal = _jwtService.ValidateToken(invalidToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithNullToken_ShouldReturnNull()
        {
            // Act
            var principal = _jwtService.ValidateToken(null);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithEmptyToken_ShouldReturnNull()
        {
            // Act
            var principal = _jwtService.ValidateToken("");

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithWhitespaceToken_ShouldReturnNull()
        {
            // Act
            var principal = _jwtService.ValidateToken("   ");

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_WithMalformedToken_ShouldReturnNull()
        {
            // Arrange
            var malformedToken = "this.is.not.a.valid.jwt.token";

            // Act
            var principal = _jwtService.ValidateToken(malformedToken);

            // Assert
            principal.Should().BeNull();
        }

        [Theory]
        [InlineData("short")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("this-is-exactly-31-characters!!")]
        public void Constructor_WithInvalidSecretKey_ShouldThrowException(string invalidKey)
        {
            // Arrange
            var invalidSettings = new JwtSettings
            {
                SecretKey = invalidKey,
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            var mockOptions = new Mock<IOptions<JwtSettings>>();
            mockOptions.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            Action act = () => new JwtService(mockOptions.Object, _mockLogger.Object);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*SecretKey must be at least 32 characters long*");
        }

        [Fact]
        public void Constructor_WithValidSecretKey_ShouldNotThrow()
        {
            // Arrange
            var validSettings = new JwtSettings
            {
                SecretKey = "this-is-exactly-32-characters!!!", // 32 characters
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };

            var mockOptions = new Mock<IOptions<JwtSettings>>();
            mockOptions.Setup(x => x.Value).Returns(validSettings);

            // Act & Assert
            Action act = () => new JwtService(mockOptions.Object, _mockLogger.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void GenerateToken_MultipleCalls_ShouldGenerateDifferentTokens()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Role = UserRole.User };

            // Act
            var token1 = _jwtService.GenerateToken(user);
            var token2 = _jwtService.GenerateToken(user);

            // Assert
            token1.Should().NotBe(token2);
        }
    }
}