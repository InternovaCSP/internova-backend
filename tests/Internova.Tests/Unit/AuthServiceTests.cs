using Internova.Api.Infrastructure.Services;
using Internova.Api.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Internova.Tests.Unit;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretUnitTestSigningKey_AtLeast32Chars!",
                ["Jwt:Issuer"] = "Internova",
                ["Jwt:Audience"] = "InternovaUsers"
            })
            .Build();

        _authService = new AuthService(config);
    }

    [Fact]
    public void HashAndVerifyPassword_WorksCorrectly()
    {
        var user = new User { Id = 1, Email = "student@internova.com", Role = "Student" };
        var hash = _authService.HashPassword(user, "Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.True(_authService.VerifyPassword(user, hash, "Password123!"));
        Assert.False(_authService.VerifyPassword(user, hash, "WrongPassword"));
    }

    [Fact]
    public void GenerateJwtToken_ReturnsThreePartJwt()
    {
        var user = new User { Id = 42, Email = "student@internova.com", Role = "Student" };
        var token = _authService.GenerateJwtToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }
}
