using Microsoft.Data.SqlClient;

namespace Orders.Api.Warehouse;

public sealed class WarehouseConnection(IConfiguration configuration)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Warehouse"));
        await connection.OpenAsync(ct);
        return connection;
    }
}
