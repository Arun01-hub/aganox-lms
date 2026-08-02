using System.Security.Claims;
using Aganox.Lms.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Aganox.Lms.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User?.FindFirstValue(ClaimTypes.Name)
                        ?? User?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                            ?? User?.FindFirstValue("email");

    public string? UserName => User?.Identity?.Name ?? Email;

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value).ToArray() ?? [];
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
