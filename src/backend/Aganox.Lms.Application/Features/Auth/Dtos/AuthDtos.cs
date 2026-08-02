namespace Aganox.Lms.Application.Features.Auth.Dtos;

public sealed record AuthResponseDto(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string UserName,
    Guid? EmployeeId,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
