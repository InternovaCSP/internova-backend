using System;

namespace Internova.Core.Entities;

/// <summary>
/// Represents a vote for a seminar request.
/// </summary>
public class SeminarVote
{
    public int VoteId { get; set; }
    public int RequestId { get; set; }
    public int StudentId { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
