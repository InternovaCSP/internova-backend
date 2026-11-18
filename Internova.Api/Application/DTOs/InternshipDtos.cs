using System.ComponentModel.DataAnnotations;
using Internova.Api.Domain.Enums;

namespace Internova.Api.Application.DTOs;

public class InternshipDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Duration { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInternshipDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string? Description { get; set; }

    public string? Duration { get; set; }

    public string? Location { get; set; }

    public string? Requirements { get; set; }

    public bool IsPublished { get; set; } = false;
}

public record ApplyInternshipRequest(int InternshipId);

public record UpdateApplicationStatusRequest(string Status);

public record ScheduleInterviewRequest(
    int ApplicationId,
    DateTime InterviewDate,
    string LocationOrLink);

public record UpdateCompanyStatusRequest(CompanyStatus Status);

public record UpdateInternshipStatusRequest(string Status);
