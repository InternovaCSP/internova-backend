namespace Internova.Core.DTOs;

public class SeminarRequestCreateDto
{
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SeminarRequestResponseDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Threshold { get; set; }
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
    public DateTime CreatedAt { get; set; }
}
