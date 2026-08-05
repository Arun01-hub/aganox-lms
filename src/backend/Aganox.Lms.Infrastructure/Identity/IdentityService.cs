using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Persistence.Context;
using Aganox.Lms.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppDbContext _db;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<AuthUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null || !user.IsActive ? null : Map(user);
    }

    public async Task<AuthUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null || !user.IsActive ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (false, ["User not found."]);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Succeeded, string? Token, string[] Errors)> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (false, null, []);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return (true, token, []);
    }

    public async Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (false, ["Invalid reset request."]);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(userId, cancellationToken);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(c => c.Type == "permission"))
            {
                permissions.Add(claim.Value);
            }
        }

        // Ensure employee link stays consistent for DTO consumers.
        _ = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        return permissions.ToList();
    }

    private static AuthUserDto Map(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.UserName ?? string.Empty, user.EmployeeId, user.MustChangePassword);

    public async Task<(bool Succeeded, Guid? UserId, string[] Errors)> CreateUserForEmployeeAsync(
        Guid employeeId,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return (false, null, ["Employee not found."]);
        }

        if (employee.UserId.HasValue)
        {
            return (false, null, ["Employee is already linked to a user account."]);
        }

        var normalizedEmail = email.Trim();
        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return (false, null, ["Email is already registered."]);
        }

        if (await _roleManager.FindByNameAsync(role) is null)
        {
            return (false, null, [$"Role '{role}' does not exist."]);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            EmployeeId = employeeId,
            MustChangePassword = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return (false, null, createResult.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, role);
        employee.UserId = user.Id;
        employee.Email = normalizedEmail;
        await _db.SaveChangesAsync(cancellationToken);

        return (true, user.Id, []);
    }

    public async Task<(bool Succeeded, string[] Errors)> LinkExistingUserAsync(
        Guid employeeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return (false, ["Employee not found."]);
        }

        if (employee.UserId.HasValue)
        {
            return (false, ["Employee is already linked to a user account."]);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return (false, ["User not found."]);
        }

        if (user.EmployeeId.HasValue && user.EmployeeId != employeeId)
        {
            return (false, ["User is already linked to another employee."]);
        }

        var otherEmployee = await _db.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id != employeeId, cancellationToken);
        if (otherEmployee is not null)
        {
            return (false, ["User is already linked to another employee."]);
        }

        user.EmployeeId = employeeId;
        employee.UserId = userId;
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            employee.Email = user.Email;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, updateResult.Errors.Select(e => e.Description).ToArray());
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, []);
    }

    public async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.Email;
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Where(x => x.IsActive).Select(x => x.Id).ToList();
    }

    public async Task<IReadOnlyList<RolePermissionsDto>> GetRolesWithPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<RolePermissionsDto>();
        foreach (var roleName in Roles.All)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .OrderBy(x => x)
                .ToList();
            result.Add(new RolePermissionsDto(role.Id, role.Name ?? roleName, role.Description, permissions));
        }

        return result;
    }

    public async Task<(bool Succeeded, string[] Errors)> SetRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return (false, ["Role not found."]);
        }

        if (string.Equals(role.Name, Roles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return (false, ["Administrator permissions cannot be modified."]);
        }

        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing.Where(c => c.Type == "permission"))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var permission in permissions)
        {
            await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
        }

        return (true, []);
    }
}
