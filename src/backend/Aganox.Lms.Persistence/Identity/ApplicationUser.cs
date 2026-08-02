using Microsoft.AspNetCore.Identity;

namespace Aganox.Lms.Persistence.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? EmployeeId { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
