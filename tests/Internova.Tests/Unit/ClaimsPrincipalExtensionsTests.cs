using System.Security.Claims;
using Internova.Api.Presentation.Extensions;

namespace Internova.Tests.Unit;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void TryGetUserId_ValidClaim_ReturnsTrueAndId()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("user_id", "15")], "Test"));

        var ok = principal.TryGetUserId(out var userId);

        Assert.True(ok);
        Assert.Equal(15, userId);
    }

    [Fact]
    public void GetUserId_MissingClaim_ThrowsUnauthorized()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }
}
