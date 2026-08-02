using Aganox.Lms.Application.Features.Auth.Dtos;
using MediatR;

namespace Aganox.Lms.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto?>;
