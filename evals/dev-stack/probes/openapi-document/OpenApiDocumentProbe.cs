using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for aspnetcore-openapi-trap. It reads the OpenAPI document of the app in Development
// and checks that a client generator gets each response, and that Production does not publish the document.
[TestClass]
[DoNotParallelize]
public sealed class OpenApiDocumentProbe
{
    private const string DocumentPath = "/openapi/v1.json";
    private static readonly Regex s_getOrderPath = new(@"/orders/\{[^}/]+\}$", RegexOptions.CultureInvariant);

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        await using var factory = Factory("Development");
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(context.CancellationToken);
    }

    [TestMethod]
    public async Task OpenApi_GetOrder_Documents404AsProblemJson()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var (path, get) = FindGetOrder(document.RootElement);

        Assert.IsTrue(get.TryGetProperty("responses", out var responses), $"GET {path} has no responses.");
        Assert.IsTrue(responses.TryGetProperty("404", out var notFound), $"GET {path} does not document a 404. Responses: {Names(responses)}.");
        Assert.IsTrue(
            notFound.TryGetProperty("content", out var content) && content.TryGetProperty("application/problem+json", out _),
            $"The 404 of GET {path} is not documented as application/problem+json: {notFound}");
    }

    [TestMethod]
    public async Task GetOrder_UnknownId_Returns404Problem()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var (path, _) = FindGetOrder(document.RootElement);
        var url = Regex.Replace(path, @"\{[^}/]+\}$", Guid.CreateVersion7().ToString());
        url = Regex.Replace(url, @"\{[^}/]*version[^}/]*\}", "1", RegexOptions.IgnoreCase);

        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"GET {url} for an unknown order.");
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType, $"GET {url} for an unknown order.");
    }

    [TestMethod]
    public async Task OpenApi_EveryOperation_HasOperationId()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (operation.Value.ValueKind != JsonValueKind.Object || !operation.Value.TryGetProperty("responses", out _))
                {
                    continue;
                }

                var id = operation.Value.TryGetProperty("operationId", out var value) ? value.GetString() : null;
                if (string.IsNullOrWhiteSpace(id))
                {
                    problems.Add($"{operation.Name.ToUpperInvariant()} {path.Name} has no operationId");
                }
                else if (!seen.Add(id))
                {
                    problems.Add($"operationId '{id}' is not unique");
                }
            }
        }

        Assert.IsTrue(seen.Count + problems.Count > 0, "The document has no operations.");
        Assert.IsTrue(problems.Count == 0, string.Join("; ", problems));
    }

    [TestMethod]
    public async Task OpenApi_InProduction_IsNotPublic()
    {
        await using var factory = Factory("Production");
        using var client = factory.CreateClient();
        var results = new List<(string Url, int Status)>();
        foreach (var url in new[] { DocumentPath, "/scalar", "/scalar/v1", "/swagger/index.html", "/swagger/v1/swagger.json" })
        {
            using var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.CancellationToken);
            results.Add((url, (int)response.StatusCode));
        }

        Assert.IsTrue(
            results.All(r => r.Status is 401 or 403 or 404),
            "In Production the document and the UI must not be public: " + string.Join(", ", results.Select(r => $"{r.Url}={r.Status}")));
    }

    private static WebApplicationFactory<Program> Factory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        });

    private static async Task<JsonDocument> ReadDocumentAsync(CancellationToken ct)
    {
        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri(DocumentPath, UriKind.Relative), ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET {DocumentPath} in Development: {text[..Math.Min(200, text.Length)]}");
        return JsonDocument.Parse(text);
    }

    private static (string Path, JsonElement Get) FindGetOrder(JsonElement document)
    {
        foreach (var path in document.GetProperty("paths").EnumerateObject())
        {
            if (s_getOrderPath.IsMatch(path.Name) && path.Value.TryGetProperty("get", out var get))
            {
                return (path.Name, get);
            }
        }

        Assert.Fail("The document has no GET /orders/{id}. Paths: " + Names(document.GetProperty("paths")));
        return default;
    }

    private static string Names(JsonElement element) => string.Join(", ", element.EnumerateObject().Select(p => p.Name));
}
