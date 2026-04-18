namespace Internova.Core.Entities;

/// <summary>
/// Represents a user in the Internova system (Student, Company, or Admin).
/// </summary>
public class User
{
    /// <summary>Identity column — maps to dbo.Users.Id (IDENTITY 1,1).</summary>
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>ASP.NET PasswordHasher output — never store plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role values: "Student", "Company", "Admin"</summary>
    public string Role { get; set; } = string.Empty;
    
    public bool EmailNotificationsEnabled { get; set; } = true;

    public bool PushNotificationsEnabled { get; set; } = true;

    public string ThemePreference { get; set; } = "system";

    public string? Bio { get; set; }

    public string? Location { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
