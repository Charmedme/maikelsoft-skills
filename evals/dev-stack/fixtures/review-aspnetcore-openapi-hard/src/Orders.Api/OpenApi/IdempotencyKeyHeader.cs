using Microsoft.OpenApi;
using Orders.Api.Infrastructure;

namespace Orders.Api.OpenApi;

public static class IdempotencyKeyHeader
{
    // IdempotencyFilter reads the header and answers 409 for a key that was used before.
    // An endpoint filter is not in the document, so the header and the 409 are added here.
    public static RouteHandlerBuilder WithIdempotencyKeyHeader(this RouteHandlerBuilder builder) =>
        builder
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = IdempotencyFilter.HeaderName,
                    In = ParameterLocation.Header,
                    Required = false,
                    Description = "A unique key for each new order (1 to 64 visible ASCII characters). A retry sends the same key.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 64 },
                });
                return Task.CompletedTask;
            });
}
