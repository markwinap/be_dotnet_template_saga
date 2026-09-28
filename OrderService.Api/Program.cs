using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using MediatR;
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
using OrderService.Application;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Commands.DeleteOrder;
using OrderService.Application.Commands.SubmitOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;
using OrderService.Application.Queries.GetOrderById;
using OrderService.Application.Queries.ListOrders;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Data;
using OrderService.Api.Swagger;
using Serilog;

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

app.MapGet("/api/orders/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender) =>
{
    var order = await sender.Send(new GetOrderByIdQuery(id, GetCustomerId(user)));
    return order is null ? Results.NotFound() : Results.Ok(order);
})
.WithName("GetOrderById")
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithSummary("Gets an order by id.")
.RequireRateLimiting("api");

app.MapGet("/api/orders", async (ClaimsPrincipal user, ISender sender) =>
{
    var orders = await sender.Send(new ListOrdersQuery(GetCustomerId(user)));
    return Results.Ok(orders);
})
.Produces<IReadOnlyCollection<OrderDto>>(StatusCodes.Status200OK)
.WithSummary("Lists orders.")
.RequireRateLimiting("api");

app.MapPost("/api/orders", async (CreateOrderRequest request, ClaimsPrincipal user, ISender sender) =>
{
    var command = new CreateOrderCommand(
        GetCustomerId(user),
        request.ShippingLine1,
        request.City,
        request.State,
        request.PostalCode,
        request.Country,
        request.Items);
    var order = await sender.Send(command);
    return Results.Created($"/api/orders/{order.Id}", order);
})
.Produces<OrderDto>(StatusCodes.Status201Created)
.WithSummary("Creates an order.")
.RequireRateLimiting("api");

app.MapPost("/api/orders/{id:guid}/submit", async (Guid id, ClaimsPrincipal user, ISender sender) =>
{
    await sender.Send(new SubmitOrderCommand(id, GetCustomerId(user)));
    return Results.Accepted($"/api/orders/{id}");
})
.WithSummary("Submits an order and starts SAGA orchestration.")
.RequireRateLimiting("api");

app.MapDelete("/api/orders/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender) =>
{
    try
    {
        await sender.Send(new DeleteOrderCommand(id, GetCustomerId(user)));
        return Results.NoContent();
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
})
.WithSummary("Deletes an order.")
.RequireRateLimiting("api");

app.MapGet("/api/orders/{id:guid}/workflow-state", async (Guid id, IOrderWorkflowExecutionStore executionStore, CancellationToken cancellationToken) =>
{
    var execution = await executionStore.GetByOrderIdAsync(id, cancellationToken);
    return execution is null ? Results.NotFound() : Results.Ok(execution);
})
.WithName("GetOrderWorkflowState")
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithSummary("Gets the durable workflow execution state for an order.")
.RequireRateLimiting("api");

app.MapGet("/api/orders/{id:guid}/audit", async (Guid id, IAuditRepository auditRepository, CancellationToken cancellationToken) =>
{
    var traces = await auditRepository.ListOrderTracesAsync(id, cancellationToken);
    return Results.Ok(traces);
})
.WithName("GetOrderAuditTrace")
.Produces<IReadOnlyCollection<object>>(StatusCodes.Status200OK)
.WithSummary("Gets the audit/state trace history for an order.")
.RequireRateLimiting("api");

app.Run();

static string GetCustomerId(ClaimsPrincipal user) =>
    user.FindFirstValue(JwtRegisteredClaimNames.Sub)
    ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");

public sealed record DevTokenRequest(string Subject);

public sealed record CreateOrderRequest(
    string ShippingLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    IReadOnlyCollection<CreateOrderItemRequest> Items);
