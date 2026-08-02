using Aganox.Lms.Application.Common.Interfaces;
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
}
