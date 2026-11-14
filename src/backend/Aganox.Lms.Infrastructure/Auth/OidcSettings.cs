namespace Aganox.Lms.Infrastructure.Auth;

public sealed class OidcSettings
{
    public const string SectionName = "Oidc";

    public bool Enabled { get; set; }

    /// <summary>Identity server base URL (e.g. https://localhost:7036).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>API resource audience expected on access tokens.</summary>
    public string Audience { get; set; } = "aganox.lms.api";

    /// <summary>Dev-only: trust self-signed TLS from the local identity server.</summary>
    public bool DisableCertificateValidation { get; set; }
}
