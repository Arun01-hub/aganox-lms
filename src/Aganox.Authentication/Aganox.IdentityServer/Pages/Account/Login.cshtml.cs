using System.ComponentModel.DataAnnotations;
using Aganox.IdentityServer.Application.Abstractions;
using Aganox.IdentityServer.Application.Models;
using Aganox.IdentityServer.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aganox.IdentityServer.Pages.Account;

public sealed class LoginModel(IUserAccountService userAccountService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet([FromQuery(Name = "ReturnUrl")] string? returnUrl = null) =>
        ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync([FromForm(Name = "returnUrl")] string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await userAccountService.SignInAsync(new LoginRequest
        {
            Email = Input.Email,
            Password = Input.Password,
            RememberMe = Input.RememberMe,
            ReturnUrl = returnUrl
        });

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "This account has been locked out.";
            return Page();
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }

    public sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
