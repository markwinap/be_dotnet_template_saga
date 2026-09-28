using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OrderService.Api.Options;
using OrderService.Api.Services;
using OrderService.Api.Swagger;
using OrderService.Application;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Common.Middleware;
using OrderService.Application.Messages;
using OrderService.Domain.Services;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Data;
using Serilog;
using JasperFx.Core;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/orderservice-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => builder.Environment.IsDevelopment() || options.Secret != JwtOptions.DevelopmentSecret,
        "The development JWT secret cannot be used outside Development.")
    .ValidateOnStart();
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<OrderServiceOptions>()
    .Bind(builder.Configuration.GetSection(OrderServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrderServiceOptions>>().Value.ToDomainOptions());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditContext, HttpAuditContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "OrderService.Api.xml"), includeControllerXmlComments: true);
    options.OperationFilter<EndpointExamplesOperationFilter>();
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Provide a valid JWT Bearer token"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient("downstream")
    .AddStandardResilienceHandler();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", failureStatus: HealthStatus.Degraded);

var openTelemetry = builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
    });

var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    openTelemetry.UseAzureMonitor(options => options.ConnectionString = applicationInsightsConnectionString);
}

var dbConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=OrderService;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
var serviceBusConnection = builder.Configuration.GetConnectionString("ServiceBus")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(OrderService.Application.DependencyInjection).Assembly);

    opts.UseFluentValidation();

    opts.PersistMessagesWithSqlServer(dbConnection);
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();

    opts.UseAzureServiceBus(serviceBusConnection)
        .AutoProvision();
    opts.PublishMessage<OrderSubmittedEvent>().ToAzureServiceBusTopic("orders.submitted");

    opts.Policies.AddMiddleware(typeof(AuditMiddleware), chain => chain.MessageType.IsAssignableTo(typeof(IAuditableCommand)));

    opts.Policies.OnException<Exception>()
        .RetryWithCooldown(250.Milliseconds(), 500.Milliseconds(), 1.Seconds())
        .WithFullJitter()
        .Then.MoveToErrorQueue();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapPrometheusScrapingEndpoint();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous().DisableHttpMetrics();
app.MapHealthChecks("/health/ready").AllowAnonymous().DisableHttpMetrics();
app.MapHealthChecks("/health").AllowAnonymous().DisableHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/auth/dev-token", ([FromBody] DevTokenRequest request) =>
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.ExpiresMinutes),
            signingCredentials: creds);

        return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
    }).AllowAnonymous().WithSummary("Development-only token helper.");
}

app.MapWolverineEndpoints();

app.Run();

public sealed record DevTokenRequest(string Subject);
