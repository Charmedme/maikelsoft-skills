using System.Globalization;
using Dapper;
using NpgsqlTypes;
using Orders.Api.Reports;

namespace Orders.Api.Import;

/// <summary>Nightly import of the stock levels from the warehouse CSV file (sku;available).</summary>
public sealed class StockImport(ReportConnection connections)
{
    public async Task<int> ImportAsync(string fileName, Stream csv, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await connection.ExecuteAsync("TRUNCATE stock_levels", transaction: transaction);

        var rows = 0;
        await using (var writer = await connection.BeginBinaryImportAsync(
            "COPY stock_levels (sku, available) FROM STDIN (FORMAT BINARY)", ct))
        {
            using var reader = new StreamReader(csv);
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                var parts = line.Split(';');
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(parts[0], NpgsqlDbType.Varchar, ct);
                await writer.WriteAsync(int.Parse(parts[1], CultureInfo.InvariantCulture), NpgsqlDbType.Integer, ct);
                rows++;
            }
        }

        await connection.ExecuteAsync(
            "INSERT INTO stock_imports (file_name, row_count, imported_at) VALUES (@fileName, @rows, @importedAt)",
            new { fileName, rows, importedAt = DateTime.Now },
            transaction);
        await transaction.CommitAsync(ct);
        return rows;
    }
}
