namespace Internova.Core.Entities;

/// <summary>
/// Represents a user in the Internova system (student, employer, or admin).
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password — never store plaintext.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Role values: "Student", "Employer", "Admin"
    /// </summary>
    public string Role { get; set; } = string.Empty;

    public string? ContactNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
