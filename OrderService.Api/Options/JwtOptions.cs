using System.ComponentModel.DataAnnotations;

namespace OrderService.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string DevelopmentSecret = "PLEASE-CHANGE-TO-AT-LEAST-32-CHARACTERS";

    [Required]
    public string Issuer { get; init; } = "OrderService";

    [Required]
    public string Audience { get; init; } = "OrderServiceClients";

    [Required, MinLength(32)]
    public string Secret { get; init; } = DevelopmentSecret;

    [Range(1, 1440)]
    public int ExpiresMinutes { get; init; } = 60;
}
