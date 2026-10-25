using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Invoices;

/// <summary>Invoice numbers are gapless for each year (a tax rule): 2026-000001, 2026-000002, ...</summary>
public static class InvoiceNumbers
{
    public static string Format(int year, long sequence) =>
        string.Create(CultureInfo.InvariantCulture, $"{year}-{sequence:000000}");

    /// <summary>
    /// Takes the next number of the year. The row lock of the UPDATE lasts until the transaction ends,
    /// so a rollback gives the number back and no gap appears. Call it in the transaction that saves the invoice.
    /// </summary>
    public static async Task<string> NextAsync(OrdersDbContext db, int year, CancellationToken ct)
    {
        var next = await db.Database.SqlQuery<long>($"""
            INSERT INTO invoice_counters ("Year", "LastNumber") VALUES ({year}, 1)
            ON CONFLICT ("Year") DO UPDATE SET "LastNumber" = invoice_counters."LastNumber" + 1
            RETURNING "LastNumber" AS "Value"
            """).ToListAsync(ct);
        return Format(year, next[0]);
    }
}
