using Aganox.IdentityServer.Application.Abstractions;
using Aganox.IdentityServer.Application.Models;
using Aganox.IdentityServer.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Aganox.IdentityServer.Services;

public sealed class UserAccountService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : IUserAccountService
{
    public async Task<LoginResult> SignInAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return LoginResult.Failure();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LoginResult.Success();
        }

        return LoginResult.Failure(result.IsLockedOut, result.RequiresTwoFactor);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default) =>
        signInManager.SignOutAsync();
}
