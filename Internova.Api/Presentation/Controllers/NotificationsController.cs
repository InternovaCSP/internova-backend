using Internova.Api.Presentation.Extensions;
using Internova.Api.Application.DTOs;
using Internova.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internova.Api.Presentation.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationRepository notificationRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var (items, totalCount, unreadCount) = await notificationRepository.GetByUserIdAsync(userId, page, pageSize);

        return Ok(new NotificationSummaryDto
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
        });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await notificationRepository.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        await notificationRepository.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}
