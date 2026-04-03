using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/seminars")]
public class SeminarController(
    ISeminarRepository seminarRepository,
    ILogger<SeminarController> logger) : ControllerBase
{
    private readonly ISeminarRepository _seminarRepository = seminarRepository;
    private readonly ILogger<SeminarController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _seminarRepository.GetAllAsync();
        
        int? currentUserId = null;
        var userIdClaim = User.FindFirstValue("user_id");
        if (int.TryParse(userIdClaim, out var id)) currentUserId = id;

        var response = new List<SeminarRequestResponseDto>();
        foreach (var r in requests)
        {
            response.Add(new SeminarRequestResponseDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentName = r.StudentName,
                Topic = r.Topic,
                Description = r.Description,
                Status = r.Status,
                Threshold = r.Threshold,
                VoteCount = r.VoteCount,
                CreatedAt = r.CreatedAt,
                HasVoted = currentUserId.HasValue && await _seminarRepository.HasStudentVotedAsync(r.Id, currentUserId.Value)
            });
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await _seminarRepository.GetByIdAsync(id);
        if (r == null) return NotFound();

        int? currentUserId = null;
        var userIdClaim = User.FindFirstValue("user_id");
        if (int.TryParse(userIdClaim, out var uid)) currentUserId = uid;

        var response = new SeminarRequestResponseDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            StudentName = r.StudentName,
            Topic = r.Topic,
            Description = r.Description,
            Status = r.Status,
            Threshold = r.Threshold,
            VoteCount = r.VoteCount,
            CreatedAt = r.CreatedAt,
            HasVoted = currentUserId.HasValue && await _seminarRepository.HasStudentVotedAsync(r.Id, currentUserId.Value)
        };

        return Ok(response);
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SeminarRequestCreateDto dto)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.Topic) || string.IsNullOrWhiteSpace(dto.Description))
        {
            return BadRequest(new { error = "Topic and Description are required." });
        }

        var request = new SeminarRequest
        {
            StudentId = userId,
            Topic = dto.Topic,
            Description = dto.Description,
            Status = "Pending",
            Threshold = 2, // Default threshold
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await _seminarRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, request);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id}/vote")]
    public async Task<IActionResult> Vote(int id)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var seminar = await _seminarRepository.GetByIdAsync(id);
        if (seminar == null) return NotFound();

        if (await _seminarRepository.HasStudentVotedAsync(id, userId))
        {
            return BadRequest(new { error = "You have already voted for this seminar request." });
        }

        var success = await _seminarRepository.VoteAsync(id, userId);
        if (!success) return BadRequest(new { error = "Failed to submit vote." });

        // Check if threshold reached
        var voteCount = await _seminarRepository.GetVoteCountAsync(id);
        if (voteCount >= seminar.Threshold && seminar.Status == "Pending")
        {
            await _seminarRepository.UpdateStatusAsync(id, "Approved");
            _logger.LogInformation("Seminar request {RequestId} reached its threshold and has been Approved.", id);
        }

        var updatedSeminar = await _seminarRepository.GetByIdAsync(id);
        return Ok(new { 
            message = "Vote submitted successfully.", 
            voteCount = updatedSeminar?.VoteCount ?? voteCount,
            status = updatedSeminar?.Status ?? "Pending"
        });
    }
}
