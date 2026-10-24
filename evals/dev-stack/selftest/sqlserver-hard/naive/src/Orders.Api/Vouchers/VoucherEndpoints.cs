using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Vouchers;

public static class VoucherEndpoints
{
    public static IEndpointRouteBuilder MapVouchers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vouchers").WithTags("Vouchers");
        group.MapPut("/{code}", PutAsync);
        group.MapGet("/{code}", GetAsync);
        return app;
    }

    private static async Task<Ok<VoucherResponse>> PutAsync(string code, PutVoucherRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = Voucher.Normalize(code);

        // One statement, like the stock receipts: create the voucher or change it.
        await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.Vouchers AS t
            USING (SELECT {normalized} AS Code, {request.PercentOff} AS PercentOff, {request.MaxUses} AS MaxUses) AS s
                ON t.Code = s.Code
            WHEN MATCHED THEN
                UPDATE SET PercentOff = s.PercentOff, MaxUses = s.MaxUses
            WHEN NOT MATCHED THEN
                INSERT (Id, Code, PercentOff, MaxUses, Used) VALUES ({Guid.CreateVersion7()}, s.Code, s.PercentOff, s.MaxUses, 0);
            """, ct);

        var voucher = await db.Vouchers.AsNoTracking().FirstAsync(v => v.Code == normalized, ct);
        return TypedResults.Ok(voucher.ToResponse());
    }

    private static async Task<Results<Ok<VoucherResponse>, NotFound>> GetAsync(string code, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = Voucher.Normalize(code);
        var voucher = await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Code == normalized, ct);
        return voucher is null ? TypedResults.NotFound() : TypedResults.Ok(voucher.ToResponse());
    }
}
