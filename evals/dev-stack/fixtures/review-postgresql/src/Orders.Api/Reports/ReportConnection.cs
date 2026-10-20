using Npgsql;

namespace Orders.Api.Reports;

public sealed class ReportConnection(IConfiguration configuration)
{
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(configuration.GetConnectionString("Orders"));
        await connection.OpenAsync(ct);
        return connection;
    }
}
