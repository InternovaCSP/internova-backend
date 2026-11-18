using System.Security.Claims;

namespace Internova.Api.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out int userId)
    {
        var claim = user.FindFirstValue("user_id");
        return int.TryParse(claim, out userId);
    }

    public static int GetUserId(this ClaimsPrincipal user) =>
        user.TryGetUserId(out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identity could not be determined.");
}
