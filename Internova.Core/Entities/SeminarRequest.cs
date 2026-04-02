using System;

namespace Internova.Core.Entities;

/// <summary>
/// Represents a seminar request created by a student.
/// </summary>
public class SeminarRequest
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Threshold { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Calculated properties
    public int VoteCount { get; set; }
}
