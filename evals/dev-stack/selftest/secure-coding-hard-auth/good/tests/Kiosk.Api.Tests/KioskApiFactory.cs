using System.Net;
using Kiosk.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Kiosk.Api.Tests;

public sealed class KioskApiFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = "kiosk_" + Guid.NewGuid().ToString("N");

    public string ConnectionString => new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString) { Database = DatabaseName }.ConnectionString;

    /// <summary>The outgoing HTTP calls of the app (profile images). A stub answers each call.</summary>
    public List<Uri> OutboundRequests { get; } = [];

    public async Task<T> QueryAsync<T>(Func<KioskDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<KioskDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Kiosk", ConnectionString);
        builder.UseSetting("Database:Initialize", "true");
        builder.UseSetting("RateLimiting:Login:PermitLimit", "1000");
        builder.UseSetting("Jwt:SigningKey", Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        builder.ConfigureServices(services =>
            services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => new StubHttpHandler(OutboundRequests))));
    }
}

public sealed class StubHttpHandler(List<Uri> requests) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (requests)
        {
            requests.Add(request.RequestUri!);
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        };
        response.Content.Headers.ContentType = new("image/png");
        return Task.FromResult(response);
    }}
