using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Orders.Api.Warehouse;

public sealed record StockLevel(string Sku, int Available);

public sealed record DailyPicks(DateOnly Day, int Picks, int Units);

public sealed class StockRepository(WarehouseConnection connections)
{
    public async Task<int?> GetAvailableAsync(string sku, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand("SELECT Available FROM dbo.StockLevels WHERE Sku = @sku", connection);
        command.Parameters.AddWithValue("@sku", sku);
        var value = await command.ExecuteScalarAsync(ct);
        return value is int available ? available : null;
    }

    /// <summary>Reserves the stock for all lines of an order, or nothing.</summary>
    public async Task<bool> ReserveAsync(IReadOnlyList<(string Sku, int Quantity)> lines, CancellationToken ct)
    {
        foreach (var (sku, quantity) in lines)
        {
            var available = await GetAvailableAsync(sku, ct);
            if (available is null || available < quantity)
            {
                return false;
            }
        }

        await using var connection = await connections.OpenAsync(ct);
        foreach (var (sku, quantity) in lines)
        {
            var available = await GetAvailableAsync(sku, ct);
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.StockLevels SET Available = @available, UpdatedAt = SYSDATETIMEOFFSET() WHERE Sku = @sku",
                new { available = available - quantity, sku = new DbString { Value = sku, IsAnsi = true, Length = 32 } },
                cancellationToken: ct));
        }

        return true;
    }

    public async Task<bool> PickAsync(string sku, int quantity, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        var parameters = new DynamicParameters();
        parameters.Add("@Sku", sku, DbType.AnsiString, size: 32);
        parameters.Add("@Quantity", quantity);
        parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await connection.ExecuteAsync(new CommandDefinition("dbo.PickStock", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return parameters.Get<int>("@Result") == 0;
    }

    public async Task UpsertAsync(string sku, int available, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            "dbo.UpsertStock",
            new { Sku = new DbString { Value = sku, IsAnsi = true, Length = 32 }, Available = available },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<StockLevel>> SearchAsync(string skuPrefix, string sortColumn, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<StockLevel>(new CommandDefinition(
            "dbo.SearchStock",
            new { SkuPrefix = new DbString { Value = skuPrefix, IsAnsi = true, Length = 32 }, SortColumn = sortColumn },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<DailyPicks>> DailyPicksAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<DailyPicks>(new CommandDefinition(
            "dbo.DailyPicks",
            new { From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
        return rows.AsList();
    }
}
