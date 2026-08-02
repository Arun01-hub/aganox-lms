using Aganox.IdentityServer.Application.Models;

namespace Aganox.IdentityServer.Application.Abstractions;

public interface IClientRegistrationService
{
    Task EnsureDevelopmentClientsAsync(CancellationToken cancellationToken = default);

    Task RegisterClientAsync(ClientRegistrationDto client, CancellationToken cancellationToken = default);
}
