using Internova.Api.Controllers;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;

namespace Internova.Tests;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// Coverage: Register, Login, Me, ChangePassword, DeleteAccount.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();

        // Provide a valid 32-char JWT key so GenerateJwt() doesn't throw
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Jwt:Key"]      = "TestSuperSecretKey_12345678901234",
            ["Jwt:Issuer"]   = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        _controller = new AuthController(_userRepoMock.Object, _configuration);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper: set an authenticated user on the controller context
    // ──────────────────────────────────────────────────────────────────────────
    private void SetUserContext(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
        };
        var identity  = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ── REGISTER ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidStudent_Returns201()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Alice Smith",
            Email    = "alice@example.com",
            Password = "Password1!",
            Role     = "Student"
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync(42);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Register_ValidCompany_Returns201()
    {
        var request = new RegisterRequest
        {
            FullName = "Acme Corp",
            Email    = "hr@acme.com",
            Password = "Password1!",
            Role     = "Company"
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync(99);

        var result = await _controller.Register(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Register_AdminRole_ReturnsBadRequest()
    {
        // Admin self-registration is forbidden
        var request = new RegisterRequest
        {
            FullName = "Evil Admin",
            Email    = "evil@example.com",
            Password = "Password1!",
            Role     = "Admin"
        };

        var result = await _controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            FullName = "Hacker",
            Email    = "h@x.com",
            Password = "Password1!",
            Role     = "SuperUser"
        };

        var result = await _controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var existing = new User { Id = 1, Email = "alice@example.com" };
        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(existing);

        var request = new RegisterRequest
        {
            FullName = "Alice Again",
            Email    = "alice@example.com",
            Password = "Password1!",
            Role     = "Student"
        };

        var result = await _controller.Register(request);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_EmailIsCaseNormalized()
    {
        // Email should be stored as lowercase
        string? capturedEmail = null;
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedEmail = u.Email)
            .ReturnsAsync(1);

        var request = new RegisterRequest
        {
            FullName = "Bob",
            Email    = "BOB@EXAMPLE.COM",
            Password = "Password1!",
            Role     = "Student"
        };

        await _controller.Register(request);

        Assert.Equal("bob@example.com", capturedEmail);
    }

    // ── LOGIN ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange — create a properly-hashed password
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user   = new User { Id = 1, Email = "alice@example.com", Role = "Student" };
        user.PasswordHash = hasher.HashPassword(user, "Password1!");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync(user);

        var request = new LoginRequest { Email = "alice@example.com", Password = "Password1!" };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal("Student", response.Role);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _controller.Login(new LoginRequest { Email = "nobody@x.com", Password = "pass" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user   = new User { Id = 2, Email = "bob@example.com", Role = "Company" };
        user.PasswordHash = hasher.HashPassword(user, "RealPassword1!");

        _userRepoMock.Setup(r => r.GetByEmailAsync("bob@example.com")).ReturnsAsync(user);

        var result = await _controller.Login(new LoginRequest { Email = "bob@example.com", Password = "WrongPassword!" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user   = new User { Id = 3, Email = "carol@example.com", Role = "Student" };
        user.PasswordHash = hasher.HashPassword(user, "SecurePass1!");

        _userRepoMock.Setup(r => r.GetByEmailAsync("carol@example.com")).ReturnsAsync(user);

        var result = await _controller.Login(new LoginRequest { Email = "CAROL@EXAMPLE.COM", Password = "SecurePass1!" });

        Assert.IsType<OkObjectResult>(result);
    }

    // ── ME ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Me_AuthenticatedUser_ReturnsOkWithClaims()
    {
        SetUserContext(5);
        var result = _controller.Me();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    // ── CHANGE PASSWORD ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_ReturnsOk()
    {
        SetUserContext(10);
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user   = new User { Id = 10, Email = "user@example.com" };
        user.PasswordHash = hasher.HashPassword(user, "OldPass1!");

        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdatePasswordAsync(10, It.IsAny<string>())).Returns(Task.CompletedTask);

        var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "NewPass2!" };
        var result  = await _controller.ChangePassword(request);

        Assert.IsType<OkObjectResult>(result);
        _userRepoMock.Verify(r => r.UpdatePasswordAsync(10, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        SetUserContext(10);
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user   = new User { Id = 10, Email = "user@example.com" };
        user.PasswordHash = hasher.HashPassword(user, "RealOldPass!");

        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

        var request = new ChangePasswordRequest { CurrentPassword = "WrongOldPass!", NewPassword = "NewPass2!" };
        var result  = await _controller.ChangePassword(request);

        Assert.IsType<BadRequestObjectResult>(result);
        _userRepoMock.Verify(r => r.UpdatePasswordAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_MissingUserIdClaim_ReturnsUnauthorized()
    {
        // No user context set → no user_id claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new ChangePasswordRequest { CurrentPassword = "Old", NewPassword = "New" };
        var result  = await _controller.ChangePassword(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── DELETE ACCOUNT ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccount_AuthenticatedUser_ReturnsOk()
    {
        SetUserContext(7);
        _userRepoMock.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteAccount();

        Assert.IsType<OkObjectResult>(result);
        _userRepoMock.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await _controller.DeleteAccount();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
