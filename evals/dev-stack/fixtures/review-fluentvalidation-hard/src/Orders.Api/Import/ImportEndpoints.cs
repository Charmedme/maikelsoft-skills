using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Import;

public static class ImportEndpoints
{
    public static RouteGroupBuilder MapImport(this RouteGroupBuilder admin)
    {
        admin.MapPost("/orders/import", ImportAsync).Accepts<string>("text/csv");
        return admin;
    }

    /// <summary>Imports a CSV export of the sales team and reports the rejected orders.</summary>
    private static async Task<Ok<ImportReport>> ImportAsync(HttpRequest request, OrderImportService service, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var csv = await reader.ReadToEndAsync(ct);
        var (orders, invalidRows) = CsvOrderReader.Read(csv);
        var report = await service.ImportAsync(orders, ct);
        return TypedResults.Ok(report with { Rejected = [.. invalidRows, .. report.Rejected] });
    }
}
