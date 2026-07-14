namespace OrderService.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OrderService";
    public string Audience { get; init; } = "OrderServiceClients";
    public string Secret { get; init; } = "PLEASE-CHANGE-TO-AT-LEAST-32-CHARACTERS";
    public int ExpiresMinutes { get; init; } = 60;
}
