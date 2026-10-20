using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Orders.Api.OpenApi;

// Adds the bearer scheme of the webshop gateway to each document.
public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };
        return Task.CompletedTask;
    }
}

// The webshop generator needs to know that cancelledAt can be null.
public sealed class NullableCancelledAtTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonPropertyInfo?.Name == "cancelledAt")
        {
            schema.Nullable = true;
        }

        return Task.CompletedTask;
    }
}
