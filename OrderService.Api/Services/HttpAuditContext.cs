using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Api.Services;

public sealed class HttpAuditContext(IHttpContextAccessor httpContextAccessor) : IAuditContext
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;
    private readonly HttpContext? _httpContext = httpContextAccessor.HttpContext;

    public string ActorId =>
        _user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? _httpContext?.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    public string? ActorEmail => _user?.FindFirstValue(ClaimTypes.Email);

    public string? ClientIp => _httpContext?.Connection.RemoteIpAddress?.ToString();

    public string? SessionId => _httpContext?.TraceIdentifier;

    public string CorrelationId =>
        _httpContext?.Items["CorrelationId"] as string
        ?? _httpContext?.TraceIdentifier
        ?? Guid.NewGuid().ToString("N");

    public string SourceSystem => "OrderService.Api";
}
