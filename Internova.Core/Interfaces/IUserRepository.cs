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

    /// <summary>Retrieves a user by their unique ID. Returns null if not found.</summary>
    Task<User?> GetByIdAsync(int id);

    /// <summary>Persists a new user and returns the generated Id.</summary>
    Task<int> CreateAsync(User user);

    /// <summary>Updates an existing user's profile details.</summary>
    Task UpdateAsync(User user);
}
