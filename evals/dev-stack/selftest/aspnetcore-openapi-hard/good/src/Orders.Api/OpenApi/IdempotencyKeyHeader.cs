using Microsoft.OpenApi;
using Orders.Api.Infrastructure;

namespace Orders.Api.OpenApi;

public static class IdempotencyKeyHeader
{
    // IdempotencyFilter reads the header and can answer 400 or 409. A filter is not in the document, so add both here.
    public static RouteHandlerBuilder WithIdempotencyKeyHeader(this RouteHandlerBuilder builder) =>
        builder
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = IdempotencyFilter.HeaderName,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "A unique key for each new order (1 to 64 visible ASCII characters). A retry sends the same key.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 64 },
                });
                return Task.CompletedTask;
            });
}
