namespace Internova.Core.DTOs;

/// <summary>Successful authentication response containing the JWT and basic user info.</summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
