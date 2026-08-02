using System.Security.Claims;

namespace Aganox.Rental.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ??
        user.FindFirstValue("sub");

    public static string? GetUserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ??
        user.FindFirstValue("name") ??
        user.FindFirstValue(ClaimTypes.Email) ??
        user.FindFirstValue("email");

    public static string? GetUserEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ??
        user.FindFirstValue("email");
}
