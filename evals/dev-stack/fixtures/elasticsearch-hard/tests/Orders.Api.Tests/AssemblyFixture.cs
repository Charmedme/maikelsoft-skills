using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions.Common;
using Testcontainers.PostgreSql;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Orders.Api.Tests;

[TestClass]
public static class AssemblyFixture
{
    private const string Password = "orders-test-password";
    private static PostgreSqlContainer? s_postgres;
    private static IContainer? s_search;
    private static string? s_searchApiKey;

    public static string ConnectionString =>
        s_postgres?.GetConnectionString() ?? throw new InvalidOperationException("The database container did not start.");

    // The search container has security on and TLS off, so the tests talk plain HTTP with an API key.
    public static Uri SearchUri =>
        s_search is null
            ? throw new InvalidOperationException("The search container did not start.")
            : new UriBuilder(Uri.UriSchemeHttp, s_search.Hostname, s_search.GetMappedPublicPort(9200)).Uri;

    public static string SearchApiKey =>
        s_searchApiKey ?? throw new InvalidOperationException("The search container did not start.");

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // FluentAssertions 7 does not detect MSTest 4. Report its failures as MSTest assertion failures.
        Services.ThrowException = message => throw new AssertFailedException(message);

        s_postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
        s_search = new ContainerBuilder("elasticsearch:9.5.5") // the version of production (compose.yml)
            .WithPortBinding(9200, true)
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("ELASTIC_PASSWORD", Password)
            .WithEnvironment("xpack.security.http.ssl.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
            .WithEnvironment("cluster.routing.allocation.disk.threshold_enabled", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(9200)
                .ForPath("/_cluster/health")
                .WithBasicAuthentication("elastic", Password)))
            .Build();
        await Task.WhenAll(s_postgres.StartAsync(context.CancellationToken), s_search.StartAsync(context.CancellationToken));
        s_searchApiKey = await CreateApiKeyAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (s_postgres is not null)
        {
            await s_postgres.DisposeAsync();
        }

        if (s_search is not null)
        {
            await s_search.DisposeAsync();
        }
    }

    // The app authenticates with an API key, as in production.
    private static async Task<string> CreateApiKeyAsync(CancellationToken ct)
    {
        using var http = new HttpClient { BaseAddress = SearchUri };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"elastic:{Password}")));
        using var response = await http.PostAsJsonAsync("/_security/api_key", new { name = "orders-tests" }, ct);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return body.RootElement.GetProperty("encoded").GetString()!;
    }
}
