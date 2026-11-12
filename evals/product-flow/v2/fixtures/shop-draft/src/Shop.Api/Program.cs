var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/orders/{id}", (int id) => Results.Ok(new { id }));
app.MapPost("/api/returns", () => Results.Accepted());

app.Run();
