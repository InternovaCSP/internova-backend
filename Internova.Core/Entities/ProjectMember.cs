namespace Internova.Core.Entities;

public class ProjectMember
{
    public int ProjectId { get; set; }
    public int StudentId { get; set; }
    public string Role { get; set; } = "Member";
}
