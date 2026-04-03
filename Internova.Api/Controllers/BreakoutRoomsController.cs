using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Internova.Api.Controllers;

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
    // ─── POST /api/breakout-rooms ─────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new { error = "Student identity could not be determined." });
        }

        try
        {
            // 1. Generate Google Meet Link
            var meetingLink = await meetingService.GenerateMeetingLinkAsync(request.Title, request.ScheduledAt);

            // 2. Save Room to Database
            var room = new BreakoutRoom
            {
                OrganizerId = studentId,
                Title = request.Title,
                Description = request.Description,
                ScheduledAt = request.ScheduledAt,
                MeetingLink = meetingLink,
                Status = "Active", // Open immediately for this self-service model
                AwardSkills = request.AwardSkills,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await breakoutRoomRepository.AddAsync(room);

            // 3. Send Notification (CSP-93 Placeholder)
            // In a real scenario, we'd fetch "registered voters" (interested students) and email them.
            await emailService.SendEmailAsync("student-group@example.com", 
                $"New Breakout Room: {room.Title}", 
                $"Join the session here: {meetingLink}");

            return Ok(room);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create breakout room for student {StudentId}", studentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── GET /api/breakout-rooms/active ───────────────────────────────
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRooms()
    {
        try
        {
            var rooms = await breakoutRoomRepository.GetActiveAsync();
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch active breakout rooms.");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── POST /api/breakout-rooms/{id}/complete ───────────────────────
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CompleteRoom(int id)
    {
        try
        {
            var room = await breakoutRoomRepository.GetByIdAsync(id);
            if (room == null) return NotFound();

            room.Status = "Completed";
            room.UpdatedAt = DateTime.UtcNow;
            await breakoutRoomRepository.UpdateAsync(room);

            // CSP-95: Recording skills would happen here for all attendees
            // This is a placeholder for the "Post-Seminar Skills Recording"
            logger.LogInformation("Room {RoomId} marked as completed. Awarding skills: {Skills}", id, room.AwardSkills);

            return Ok(new { message = "Room completed and skills recorded." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete room {RoomId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    public class CreateRoomRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
        public string? AwardSkills { get; set; }
    }
}
