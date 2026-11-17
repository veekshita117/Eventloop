namespace Eventloop.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "EventloopApi";
    public string Audience { get; set; } = "EventloopApp";
    public int ExpiryInMinutes { get; set; } = 1440; // 24 hours
}
