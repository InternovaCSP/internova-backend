using Internova.Core.Enums;

namespace Internova.Core.Entities;

/// <summary>
/// Represents a Company's profile information.
/// company_id matches user_id in the Users table.
/// </summary>
public class CompanyProfile
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsVerified { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Pending;
}
