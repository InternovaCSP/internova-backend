namespace Internova.Core.Entities;

public class ProjectRequest
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int StudentId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
