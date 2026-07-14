using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OrderService.Api.Options;
using OrderService.Application;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Commands.DeleteOrder;
using OrderService.Application.Commands.SubmitOrder;
using OrderService.Application.Common.Exceptions;
using OrderService.Application.DTOs;
using OrderService.Application.Queries.GetOrderById;
using OrderService.Application.Queries.ListOrders;
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

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", failureStatus: HealthStatus.Degraded);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddPrometheusExporter();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapPrometheusScrapingEndpoint();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous().DisableHttpMetrics();
app.MapHealthChecks("/health/ready").AllowAnonymous().DisableHttpMetrics();
app.MapHealthChecks("/health").AllowAnonymous().DisableHttpMetrics();

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

app.MapGet("/api/orders/{id:guid}", async (Guid id, ISender sender) =>
{
    var order = await sender.Send(new GetOrderByIdQuery(id));
    return order is null ? Results.NotFound() : Results.Ok(order);
})
.WithName("GetOrderById")
.Produces<OrderDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithSummary("Gets an order by id.");

app.MapGet("/api/orders", async (ISender sender) =>
{
    var orders = await sender.Send(new ListOrdersQuery());
    return Results.Ok(orders);
})
.Produces<IReadOnlyCollection<OrderDto>>(StatusCodes.Status200OK)
.WithSummary("Lists orders.");

app.MapPost("/api/orders", async (CreateOrderCommand command, ISender sender) =>
{
    var order = await sender.Send(command);
    return Results.Created($"/api/orders/{order.Id}", order);
})
.Produces<OrderDto>(StatusCodes.Status201Created)
.WithSummary("Creates an order.");

app.MapPost("/api/orders/{id:guid}/submit", async (Guid id, ISender sender) =>
{
    await sender.Send(new SubmitOrderCommand(id));
    return Results.Accepted($"/api/orders/{id}");
})
.WithSummary("Submits an order and starts SAGA orchestration.");

app.MapDelete("/api/orders/{id:guid}", async (Guid id, ISender sender) =>
{
    try
    {
        await sender.Send(new DeleteOrderCommand(id));
        return Results.NoContent();
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
})
.WithSummary("Deletes an order.");

app.Run();

public sealed record DevTokenRequest(string Subject);
