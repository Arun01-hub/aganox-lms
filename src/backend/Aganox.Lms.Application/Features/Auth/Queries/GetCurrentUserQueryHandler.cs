using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Auth.Dtos;
using MediatR;

namespace Aganox.Lms.Application.Features.Auth.Queries;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IIdentityService identity)
    {
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return null;
        }

        var user = await _identity.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await _identity.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _identity.GetPermissionsAsync(user.Id, cancellationToken);

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.UserName,
            user.EmployeeId,
            user.MustChangePassword,
            roles,
            permissions);
    }
}
