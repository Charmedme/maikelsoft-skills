using Npgsql;

var builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Orders.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();
app.MapGet("/orders/count", async (NpgsqlDataSource db, CancellationToken cancellationToken) =>
{
    await using var command = db.CreateCommand("SELECT count(*) FROM orders;");
    return Results.Ok((long)(await command.ExecuteScalarAsync(cancellationToken))!);
});
app.MapPost("/orders", async (NpgsqlDataSource db, CreateOrder request, CancellationToken cancellationToken) =>
{
    await using var command = db.CreateCommand("INSERT INTO orders (total) VALUES ($1) RETURNING id;");
    command.Parameters.AddWithValue(request.Total);
    int id = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    return Results.Created($"/orders/{id}", new { id });
});
app.Run();

internal sealed record CreateOrder(decimal Total);
