using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

/// <summary>
/// Contract for user persistence operations.
/// Core layer — no EF Core or infrastructure dependencies.
/// </summary>
public interface IUserRepository
{
    /// <summary>Retrieves a user by their email address. Returns null if not found.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Persists a new user and returns the generated Id.</summary>
    Task<int> CreateAsync(User user);
}
