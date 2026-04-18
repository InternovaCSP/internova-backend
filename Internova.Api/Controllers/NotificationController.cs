using Internova.Core.DTOs;
using Internova.Core.Entities;
using Internova.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internova.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController(INotificationRepository notificationRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var (items, totalCount, unreadCount) = await notificationRepository.GetByUserIdAsync(userId, page, pageSize);

        var response = new NotificationSummaryDto
        {
            Items = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Content = n.Content,
                TargetUrl = n.TargetUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }),
            TotalCount = totalCount,
            UnreadCount = unreadCount
        };

        return Ok(response);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await notificationRepository.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await notificationRepository.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirstValue("user_id");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User identification missing or invalid.");
        }
        return userId;
    }
}
