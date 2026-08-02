using FluentValidation;

namespace Aganox.Lms.Application.Features.Auth.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
