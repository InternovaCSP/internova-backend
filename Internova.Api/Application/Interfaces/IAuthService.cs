using Internova.Api.Domain.Entities;

namespace Internova.Api.Application.Interfaces;

/// <summary>
/// Handles password hashing, verification, and JWT token generation.
/// </summary>
public interface IAuthService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string passwordHash, string providedPassword);
    string GenerateJwtToken(User user);
}
