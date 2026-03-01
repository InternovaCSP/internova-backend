using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

/// <summary>
/// Repository contract for StudentProfile persistence.
/// </summary>
public interface IStudentProfileRepository
{
    /// <summary>
    /// Retrieves the profile for a given user, or null if none exists yet.
    /// </summary>
    Task<StudentProfile?> GetByUserIdAsync(int userId);

    /// <summary>
    /// Inserts a new profile or updates an existing one for the same UserId.
    /// Returns the persisted profile (with Id populated on insert).
    /// </summary>
    Task<StudentProfile> UpsertAsync(StudentProfile profile);
}
