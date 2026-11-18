using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Internova.Api.Infrastructure.Services;

public class AuthService(IConfiguration configuration) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password) =>
        _passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string passwordHash, string providedPassword) =>
        _passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword) != PasswordVerificationResult.Failed;

    public string GenerateJwtToken(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "Internova";
        var audience = configuration["Jwt:Audience"] ?? "InternovaUsers";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
