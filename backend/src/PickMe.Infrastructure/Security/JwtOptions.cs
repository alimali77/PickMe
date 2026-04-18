namespace PickMe.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTtlMinutes { get; set; } = 60;
    public int RefreshTtlDays { get; set; } = 7;
}
