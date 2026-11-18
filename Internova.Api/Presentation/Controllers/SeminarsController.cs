using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/seminars")]
public class SeminarsController(
    ISeminarRepository seminarRepository,
    ILogger<SeminarsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await seminarRepository.GetAllAsync();
        var votedIds = User.TryGetUserId(out var currentUserId)
            ? await seminarRepository.GetVotedRequestIdsAsync(currentUserId)
            : new HashSet<int>();

        return Ok(requests.Select(r => ToResponseDto(r, votedIds.Contains(r.Id))));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await seminarRepository.GetByIdAsync(id);
        if (request is null)
            return NotFound();

        var hasVoted = User.TryGetUserId(out var currentUserId) &&
                       await seminarRepository.HasStudentVotedAsync(request.Id, currentUserId);

        return Ok(ToResponseDto(request, hasVoted));
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SeminarRequestCreateDto dto)
    {
        var userId = User.GetUserId();

        if (string.IsNullOrWhiteSpace(dto.Topic) || string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { error = "Topic and Description are required." });

        var now = DateTime.UtcNow;
        var request = new SeminarRequest
        {
            StudentId = userId,
            Topic = dto.Topic,
            Description = dto.Description,
            Status = "Pending",
            Threshold = 2,
            CreatedAt = now,
            UpdatedAt = now
        };

        var id = await seminarRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, request);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:int}/vote")]
    public async Task<IActionResult> Vote(int id)
    {
        var userId = User.GetUserId();

        var seminar = await seminarRepository.GetByIdAsync(id);
        if (seminar is null)
            return NotFound();

        if (await seminarRepository.HasStudentVotedAsync(id, userId))
            return BadRequest(new { error = "You have already voted for this seminar request." });

        var voted = await seminarRepository.VoteAsync(id, userId);
        if (!voted)
            return BadRequest(new { error = "Failed to submit vote." });

        var voteCount = await seminarRepository.GetVoteCountAsync(id);
        if (voteCount >= seminar.Threshold && seminar.Status == "Pending")
        {
            await seminarRepository.UpdateStatusAsync(id, "Approved");
            logger.LogInformation("Seminar request {RequestId} reached its threshold and has been Approved.", id);
        }

        var updatedSeminar = await seminarRepository.GetByIdAsync(id);
        return Ok(new
        {
            message = "Vote submitted successfully.",
            voteCount = updatedSeminar?.VoteCount ?? voteCount,
            status = updatedSeminar?.Status ?? "Pending"
        });
    }

    private static SeminarRequestResponseDto ToResponseDto(SeminarRequest r, bool hasVoted) => new()
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
        HasVoted = hasVoted
    };
}
