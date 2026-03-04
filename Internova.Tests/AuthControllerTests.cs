using Xunit;
using Moq;
using Internova.Api.Controllers;
using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Internova.Tests;

/// <summary>
/// xUnit Tests for AuthController
/// Test Cases: TC-AUTH-001, TC-AUTH-002, TC-AUTH-003, TC-AUTH-004, TC-AUTH-009, TC-AUTH-010
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new AuthController(_mockUserRepository.Object, _mockConfiguration.Object);
    }

    // ───────── TC-AUTH-001: Valid Registration ─────────
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "SecurePass123!",
            Role = "Student"
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);
        _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    // ───────── TC-AUTH-002: Registration with Duplicate Email ─────────
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Jane Doe",
            Email = "existing@example.com",
            Password = "SecurePass123!",
            Role = "Student"
        };

        var existingUser = new User { Id = 1, Email = "existing@example.com" };
        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    // ───────── TC-AUTH-003: Invalid Email Format ─────────
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest(string invalidEmail)
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = invalidEmail,
            Password = "SecurePass123!",
            Role = "Student"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badResult.StatusCode);
    }

    // ───────── TC-AUTH-004: Weak Password ─────────
    [Theory]
    [InlineData("123")]
    [InlineData("weak")]
    [InlineData("")]
    public async Task Register_WithWeakPassword_ReturnsBadRequest(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = weakPassword,
            Role = "Student"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert - Should fail model validation
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ───────── TC-AUTH-008: Admin Role Self-Registration Prevention ─────────
    [Fact]
    public async Task Register_WithAdminRole_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Malicious User",
            Email = "admin@example.com",
            Password = "SecurePass123!",
            Role = "Admin"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badResult.StatusCode);
    }

    // ───────── TC-AUTH-009: Valid Login ─────────
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "SecurePass123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            Role = "Student",
            FullName = "John Doe"
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "SecurePass123!");

        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _mockConfiguration.Setup(c => c["Jwt:Key"])
            .Returns("ThisIsAVeryLongSecureKeyWith32CharactersMinimum!");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    // ───────── TC-AUTH-010: Login with Wrong Password ─────────
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            Role = "Student"
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "SecurePass123!");

        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    // ───────── TC-AUTH-011: Login with Non-Existent Email ─────────
    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SecurePass123!"
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _controller.Login(request);

        // Assert (Generic error - no email enumeration)
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    // ───────── TC-AUTH-019: Verify Password is Hashed ─────────
    [Fact]
    public void PasswordHasher_HashesProperly()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var hasher = new PasswordHasher<User>();
        var plainPassword = "SecurePass123!";

        // Act
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Assert
        Assert.NotEqual(plainPassword, hashedPassword);
        Assert.True(hashedPassword.Length > 20);
        var result = hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);
        Assert.Equal(PasswordVerificationResult.Success, result);
    }
}
