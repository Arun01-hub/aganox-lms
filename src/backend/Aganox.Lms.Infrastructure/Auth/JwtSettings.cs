namespace Aganox.Lms.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Aganox.Lms";
    public string Audience { get; set; } = "Aganox.Lms.Clients";
    public string Key { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+";
    public int AccessTokenMinutes { get; set; } = 60;
}
