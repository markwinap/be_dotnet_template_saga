using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderService.Api.Swagger;

public sealed class EndpointExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.TrimStart('/').ToLowerInvariant();
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();

        if (path is null || method is null)
        {
            return;
        }

        if (path == "api/auth/dev-token" && method == "POST")
        {
            AddJsonRequestExample(operation, new OpenApiObject
            {
                ["subject"] = new OpenApiString("local-dev-user")
            });

            AddJsonResponseExample(operation, "200", new OpenApiObject
            {
                ["accessToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJsb2NhbC1kZXYtdXNlciJ9.signature")
            });
        }

        if (path == "api/orders" && method == "POST")
        {
            AddJsonRequestExample(operation, new OpenApiObject
            {
                ["customerId"] = new OpenApiString("customer-123"),
                ["shippingLine1"] = new OpenApiString("1 Main St"),
                ["city"] = new OpenApiString("Seattle"),
                ["state"] = new OpenApiString("WA"),
                ["postalCode"] = new OpenApiString("98052"),
                ["country"] = new OpenApiString("US"),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["productCode"] = new OpenApiString("SKU-1"),
                        ["quantity"] = new OpenApiInteger(2),
                        ["unitPrice"] = new OpenApiDouble(25.00),
                        ["currency"] = new OpenApiString("USD")
                    }
                }
            });

            AddJsonResponseExample(operation, "201", new OpenApiObject
            {
                ["id"] = new OpenApiString("4a18f3d2-a96d-4d4d-97a2-a5657f5f2d36"),
                ["orderNumber"] = new OpenApiString("ORD-20260709220000-1234"),
                ["customerId"] = new OpenApiString("customer-123"),
                ["status"] = new OpenApiString("Draft"),
                ["createdAtUtc"] = new OpenApiDateTime(DateTime.Parse("2026-07-09T22:00:00Z")),
                ["total"] = new OpenApiDouble(50.00),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["productCode"] = new OpenApiString("SKU-1"),
                        ["quantity"] = new OpenApiInteger(2),
                        ["unitPrice"] = new OpenApiDouble(25.00),
                        ["currency"] = new OpenApiString("USD")
                    }
                }
            });
        }

        if (path == "api/orders" && method == "GET")
        {
            AddJsonResponseExample(operation, "200", new OpenApiArray
            {
                new OpenApiObject
                {
                    ["id"] = new OpenApiString("4a18f3d2-a96d-4d4d-97a2-a5657f5f2d36"),
                    ["orderNumber"] = new OpenApiString("ORD-20260709220000-1234"),
                    ["customerId"] = new OpenApiString("customer-123"),
                    ["status"] = new OpenApiString("Draft"),
                    ["createdAtUtc"] = new OpenApiDateTime(DateTime.Parse("2026-07-09T22:00:00Z")),
                    ["total"] = new OpenApiDouble(50.00),
                    ["items"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["productCode"] = new OpenApiString("SKU-1"),
                            ["quantity"] = new OpenApiInteger(2),
                            ["unitPrice"] = new OpenApiDouble(25.00),
                            ["currency"] = new OpenApiString("USD")
                        }
                    }
                }
            });
        }

        if (path == "api/orders/{id:guid}" && method == "GET")
        {
            AddJsonResponseExample(operation, "200", new OpenApiObject
            {
                ["id"] = new OpenApiString("4a18f3d2-a96d-4d4d-97a2-a5657f5f2d36"),
                ["orderNumber"] = new OpenApiString("ORD-20260709220000-1234"),
                ["customerId"] = new OpenApiString("customer-123"),
                ["status"] = new OpenApiString("Draft"),
                ["createdAtUtc"] = new OpenApiDateTime(DateTime.Parse("2026-07-09T22:00:00Z")),
                ["total"] = new OpenApiDouble(50.00),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["productCode"] = new OpenApiString("SKU-1"),
                        ["quantity"] = new OpenApiInteger(2),
                        ["unitPrice"] = new OpenApiDouble(25.00),
                        ["currency"] = new OpenApiString("USD")
                    }
                }
            });

            AddJsonResponseExample(operation, "404", new OpenApiObject
            {
                ["message"] = new OpenApiString("Order not found")
            });
        }

        if (path == "api/orders/{id:guid}/submit" && method == "POST")
        {
            AddJsonResponseExample(operation, "202", new OpenApiObject
            {
                ["message"] = new OpenApiString("Order accepted for submission")
            });
        }

        if (path == "api/orders/{id:guid}" && method == "DELETE")
        {
            AddJsonResponseExample(operation, "204", new OpenApiObject
            {
                ["message"] = new OpenApiString("Order deleted")
            });

            AddJsonResponseExample(operation, "404", new OpenApiObject
            {
                ["message"] = new OpenApiString("Order not found")
            });
        }
    }

    private static void AddJsonRequestExample(OpenApiOperation operation, IOpenApiAny example)
    {
        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = example;
        }
    }

    private static void AddJsonResponseExample(OpenApiOperation operation, string statusCode, IOpenApiAny example)
    {
        if (!operation.Responses.TryGetValue(statusCode, out var response))
        {
            return;
        }

        if (response.Content is null)
        {
            return;
        }

        if (response.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = example;
        }
    }
}
