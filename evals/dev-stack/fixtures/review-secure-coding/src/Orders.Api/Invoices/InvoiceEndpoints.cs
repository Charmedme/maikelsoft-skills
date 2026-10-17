using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoices(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders/{orderId:guid}/invoice").WithTags("Invoices").RequireAuthorization();
        group.MapPost("/", UploadAsync).DisableAntiforgery();
        return app;
    }

    private static async Task<Results<Created, BadRequest<string>, ProblemHttpResult>> UploadAsync(
        Guid orderId,
        IFormFile file,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (file.ContentType != "application/pdf")
        {
            return TypedResults.BadRequest("Only PDF files are allowed.");
        }

        try
        {
            var folder = Path.Combine(env.WebRootPath ?? "wwwroot", "invoices", orderId.ToString());
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, file.FileName);
            await using var stream = File.Create(path);
            await file.CopyToAsync(stream, ct);
            return TypedResults.Created($"/invoices/{orderId}/{file.FileName}");
        }
        catch (IOException ex)
        {
            return TypedResults.Problem(detail: ex.ToString(), statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
