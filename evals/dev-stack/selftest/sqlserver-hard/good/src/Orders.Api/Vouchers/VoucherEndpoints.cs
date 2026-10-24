using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
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

    private static async Task<Results<Ok<VoucherResponse>, ValidationProblem>> PutAsync(
        string code,
        PutVoucherRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        if (!Voucher.IsValidCode(code))
        {
            return InvalidCode();
        }

        var normalized = Voucher.Normalize(code);

        // The marketing tool repeats a call that gets no answer, so two calls can create the same code
        // at the same time. HOLDLOCK keeps a key-range lock on the missing code: the second call waits
        // and then updates, instead of a second insert (error 2627). The parameter is varchar like the column.
        var codeParameter = new SqlParameter("@code", SqlDbType.VarChar, Voucher.MaxCodeLength) { Value = normalized };
        await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.Vouchers WITH (HOLDLOCK) AS t
            USING (VALUES ({codeParameter}, {request.PercentOff}, {request.MaxUses})) AS s (Code, PercentOff, MaxUses)
                ON t.Code = s.Code
            WHEN MATCHED THEN
                UPDATE SET PercentOff = s.PercentOff, MaxUses = s.MaxUses
            WHEN NOT MATCHED THEN
                INSERT (Code, PercentOff, MaxUses, Used) VALUES (s.Code, s.PercentOff, s.MaxUses, 0);
            """, ct);

        var voucher = await db.Vouchers.AsNoTracking().FirstAsync(v => v.Code == normalized, ct);
        return TypedResults.Ok(voucher.ToResponse());
    }

    private static async Task<Results<Ok<VoucherResponse>, NotFound, ValidationProblem>> GetAsync(string code, OrdersDbContext db, CancellationToken ct)
    {
        if (!Voucher.IsValidCode(code))
        {
            return InvalidCode();
        }

        var normalized = Voucher.Normalize(code);
        var voucher = await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Code == normalized, ct);
        return voucher is null ? TypedResults.NotFound() : TypedResults.Ok(voucher.ToResponse());
    }

    private static ValidationProblem InvalidCode() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["code"] = [$"A voucher code has 3 to {Voucher.MaxCodeLength} letters, digits or hyphens."],
        });
}
