using System.Security.Claims;

namespace NotificationService.Domain.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new InvalidOperationException($"{ClaimTypes.NameIdentifier} claim missing.");

        return long.Parse(value);
    }
}
