using Microsoft.AspNetCore.Identity;

namespace Aganox.IdentityServer.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
