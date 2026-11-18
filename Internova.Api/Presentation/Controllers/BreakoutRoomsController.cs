using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Domain.Entities;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

/// <summary>
/// API Controller for student-led Breakout Rooms.
/// </summary>
[ApiController]
[Route("api/breakout-rooms")]
[Authorize]
public class BreakoutRoomsController(
    IBreakoutRoomRepository breakoutRoomRepository,
    IMeetingService meetingService,
    IEmailService emailService,
    ILogger<BreakoutRoomsController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateBreakoutRoomRequest request)
    {
        var studentId = User.GetUserId();
        var meetingLink = await meetingService.GenerateMeetingLinkAsync(request.Title, request.ScheduledAt);
        var now = DateTime.UtcNow;

        var room = new BreakoutRoom
        {
            OrganizerId = studentId,
            Title = request.Title,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            MeetingLink = meetingLink,
            Status = "Active",
            AwardSkills = request.AwardSkills,
            CreatedAt = now,
            UpdatedAt = now
        };

        await breakoutRoomRepository.AddAsync(room);

        await emailService.SendEmailAsync(
            "student-group@example.com",
            $"New Breakout Room: {room.Title}",
            $"Join the session here: {meetingLink}");

        return Ok(room);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRooms() =>
        Ok(await breakoutRoomRepository.GetActiveAsync());

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CompleteRoom(int id)
    {
        var room = await breakoutRoomRepository.GetByIdAsync(id);
        if (room is null)
            return NotFound();

        room.Status = "Completed";
        room.UpdatedAt = DateTime.UtcNow;
        await breakoutRoomRepository.UpdateAsync(room);

        logger.LogInformation("Room {RoomId} marked as completed. Awarding skills: {Skills}", id, room.AwardSkills);
        return Ok(new { message = "Room completed and skills recorded." });
    }
}
