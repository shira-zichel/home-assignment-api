using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HomeAssignment.Controllers;
using HomeAssignment.Services;
using HomeAssignment.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HomeAssignment.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            var expectedResponse = new LoginResponse
            {
                Token = "fake-jwt-token",
                Username = "testuser",
                Role = UserRole.User,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService.Setup(x => x.LoginAsync(request))
                           .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };

            _mockAuthService.Setup(x => x.LoginAsync(request))
                           .ReturnsAsync((LoginResponse)null);

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var value = unauthorizedResult.Value;
            value.Should().NotBeNull();
            value.ToString().Should().Contain("Invalid username or password");
        }

        [Fact]
        public async Task Login_WithNullUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = null, Password = "password123" };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var value = badRequestResult.Value;
            value.Should().NotBeNull();
            value.ToString().Should().Contain("Username and password are required");
        }

        [Fact]
        public async Task Login_WithEmptyUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "", Password = "password123" };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithWhitespaceUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "   ", Password = "password123" };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithNullPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = null };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithEmptyPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "" };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithWhitespacePassword_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "   " };

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithAdminCredentials_ShouldReturnOkWithAdminToken()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "admin123" };
            var expectedResponse = new LoginResponse
            {
                Token = "admin-jwt-token",
                Username = "admin",
                Role = UserRole.Admin,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService.Setup(x => x.LoginAsync(request))
                           .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as LoginResponse;
            response.Should().NotBeNull();
            response.Role.Should().Be(UserRole.Admin);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_WithValidToken_ShouldReturnOkWithUserInfo()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            _mockAuthService.Setup(x => x.GetUserByIdAsync(1))
                           .ReturnsAsync(user);

            // Setup the User claims for the controller
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var value = okResult.Value;
            value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCurrentUser_WithInvalidUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetCurrentUser_WithMissingUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetCurrentUser_WithNonExistentUser_ShouldReturnNotFound()
        {
            // Arrange
            _mockAuthService.Setup(x => x.GetUserByIdAsync(999))
                           .ReturnsAsync((User)null);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "999"),
                new Claim(ClaimTypes.Name, "nonexistent")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region GetTestCredentials Tests

        [Fact]
        public void GetTestCredentials_ShouldReturnOkWithTestUserInfo()
        {
            // Act
            var result = _controller.GetTestCredentials();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var value = okResult.Value;
            value.Should().NotBeNull();

            // Access the actual properties of the anonymous object
            var responseType = value.GetType();
            var testUsersProperty = responseType.GetProperty("TestUsers");
            var testUsers = testUsersProperty?.GetValue(value) as Array;

            testUsers.Should().NotBeNull();
            testUsers.Length.Should().Be(2);

            // Check if admin and user are in the test users
            var testUsersList = testUsers.Cast<object>().ToList();
            var hasAdminUser = testUsersList.Any(u =>
            {
                var usernameProperty = u.GetType().GetProperty("Username");
                var username = usernameProperty?.GetValue(u) as string;
                return username == "admin";
            });

            var hasRegularUser = testUsersList.Any(u =>
            {
                var usernameProperty = u.GetType().GetProperty("Username");
                var username = usernameProperty?.GetValue(u) as string;
                return username == "user";
            });

            hasAdminUser.Should().BeTrue();
            hasRegularUser.Should().BeTrue();
        }

        [Fact]
        public void GetTestCredentials_ShouldIncludeInstructions()
        {
            // Act
            var result = _controller.GetTestCredentials();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var value = okResult.Value;
            value.Should().NotBeNull();

            // Access the Instructions property
            var responseType = value.GetType();
            var instructionsProperty = responseType.GetProperty("Instructions");
            var instructions = instructionsProperty?.GetValue(value) as string[];

            instructions.Should().NotBeNull();
            instructions.Should().Contain(instruction => instruction.Contains("POST /api/auth/login"));
            instructions.Should().Contain(instruction => instruction.Contains("Authorization: Bearer"));
        }

        #endregion

        #region Service Call Verification Tests

        [Fact]
        public async Task Login_ShouldCallAuthServiceOnce()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            _mockAuthService.Setup(x => x.LoginAsync(request))
                           .ReturnsAsync(new LoginResponse { Token = "token" });

            // Act
            await _controller.Login(request);

            // Assert
            _mockAuthService.Verify(x => x.LoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUser_ShouldCallAuthServiceOnce()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser", Role = UserRole.User };
            _mockAuthService.Setup(x => x.GetUserByIdAsync(1))
                           .ReturnsAsync(user);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act
            await _controller.GetCurrentUser();

            // Assert
            _mockAuthService.Verify(x => x.GetUserByIdAsync(1), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Login_WhenAuthServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            _mockAuthService.Setup(x => x.LoginAsync(request))
                           .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.Login(request));
        }

        [Fact]
        public async Task GetCurrentUser_WhenAuthServiceThrows_ShouldPropagateException()
        {
            // Arrange
            _mockAuthService.Setup(x => x.GetUserByIdAsync(1))
                           .ThrowsAsync(new Exception("Service error"));

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetCurrentUser());
        }

        #endregion
    }
}