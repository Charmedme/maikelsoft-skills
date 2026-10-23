using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for aspnetcore-openapi-hard. It reads the OpenAPI document that a partner generates a client from.
// The document comes from /openapi/v1.json in Development. When that route does not exist, the probe reads the
// committed build-time document (the largest *.json file with "openapi": "3.x" outside bin/ and obj/).
[TestClass]
[DoNotParallelize]
public sealed class OpenApiHardProbe
{
    private static readonly Regex s_getOrder = new(@"^/orders/\{[^}/]+\}$", RegexOptions.CultureInvariant);
    private static readonly Regex s_cancelOrder = new(@"^/orders/\{[^}/]+\}/cancel$", RegexOptions.CultureInvariant);
    private static readonly Regex s_invoice = new(@"^/orders/\{[^}/]+\}/invoice$", RegexOptions.CultureInvariant);
    private static string? s_document;

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task OpenApi_EveryOperation_HasSummaryOrDescription()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var missing = Operations(document.RootElement)
            .Where(op => string.IsNullOrWhiteSpace(Text(op.Operation, "summary")) && string.IsNullOrWhiteSpace(Text(op.Operation, "description")))
            .Select(op => $"{op.Method} {op.Path}")
            .ToList();

        Assert.IsEmpty(missing, "Operations without a summary or description: " + string.Join(", ", missing));
    }

    [TestMethod]
    public async Task OpenApi_AdminEndpoints_AreNotInPartnerDocument()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var admin = Operations(document.RootElement).Where(op => op.Path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)).Select(op => op.Path).ToList();

        Assert.IsEmpty(admin, "The partner document shows admin paths: " + string.Join(", ", admin));
    }

    [TestMethod]
    public async Task OpenApi_GetOrder_Documents404AsProblemJson()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var op = Find(document.RootElement, "get", s_getOrder);

        AssertProblem(op, "404");
    }

    [TestMethod]
    public async Task OpenApi_CancelOrder_Documents404And409AsProblemJson()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var op = Find(document.RootElement, "post", s_cancelOrder);

        AssertProblem(op, "404");
        AssertProblem(op, "409");
    }

    [TestMethod]
    public async Task OpenApi_OperationsWithBody_Document400AsProblemJson()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var problems = new List<string>();
        foreach (var op in Operations(document.RootElement).Where(op => op.Operation.TryGetProperty("requestBody", out _)))
        {
            if (!HasProblemJson(op.Operation, "400"))
            {
                problems.Add($"{op.Method} {op.Path}");
            }
        }

        Assert.IsEmpty(problems, "Operations with a request body and no 400 application/problem+json: " + string.Join(", ", problems));
    }

    [TestMethod]
    public async Task OpenApi_CreateOrder_DocumentsIdempotencyKeyHeaderAnd409()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var op = Find(document.RootElement, "post", new Regex("^/orders/?$", RegexOptions.CultureInvariant));

        var header = op.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray().FirstOrDefault(p =>
                string.Equals(Text(p, "in"), "header", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Text(p, "name"), "Idempotency-Key", StringComparison.OrdinalIgnoreCase))
            : default;

        Assert.AreEqual(JsonValueKind.Object, header.ValueKind, $"POST /orders does not document the Idempotency-Key header. Parameters: {(op.TryGetProperty("parameters", out var all) ? all.ToString() : "none")}");
        Assert.IsTrue(header.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True, $"The Idempotency-Key header is not required: {header}");
        AssertProblem(op, "409");
    }

    [TestMethod]
    public async Task OpenApi_Invoice_DocumentsPdfAnd404Problem()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var op = Find(document.RootElement, "get", s_invoice);

        Assert.IsTrue(
            op.TryGetProperty("responses", out var responses) && responses.TryGetProperty("200", out var ok)
            && ok.TryGetProperty("content", out var content) && content.TryGetProperty("application/pdf", out _),
            $"GET invoice does not document a 200 with application/pdf: {op}");
        AssertProblem(op, "404");
    }

    [TestMethod]
    public async Task OpenApi_EveryOperation_HasUniqueOperationId()
    {
        using var document = await ReadDocumentAsync(TestContext.CancellationToken);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var op in Operations(document.RootElement))
        {
            var id = Text(op.Operation, "operationId");
            if (string.IsNullOrWhiteSpace(id))
            {
                problems.Add($"{op.Method} {op.Path} has no operationId");
            }
            else if (!seen.Add(id))
            {
                problems.Add($"operationId '{id}' is not unique");
            }
        }

        Assert.IsNotEmpty(seen, "The document has no operations.");
        Assert.IsEmpty(problems, string.Join("; ", problems));
    }

    [TestMethod]
    public async Task OpenApi_InProduction_IsNotPublic()
    {
        await using var factory = Factory("Production", AssemblyFixture.ConnectionString);
        using var client = factory.CreateClient();
        var results = new List<(string Url, int Status)>();
        foreach (var url in new[] { "/openapi/v1.json", "/scalar", "/scalar/v1", "/swagger/index.html", "/swagger/v1/swagger.json" })
        {
            using var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.CancellationToken);
            results.Add((url, (int)response.StatusCode));
        }

        Assert.IsTrue(
            results.All(r => r.Status is 401 or 403 or 404),
            "In Production the document and the UI must not be public: " + string.Join(", ", results.Select(r => $"{r.Url}={r.Status}")));
    }

    // The service migrates its database at startup. The deploy pipeline has no other migration step,
    // so a normal start in Production must still create the schema of a new database.
    [TestMethod]
    public async Task Startup_InProduction_StillMigratesNewDatabase()
    {
        var connection = new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
        {
            Database = "probe_" + Guid.CreateVersion7().ToString("N")[..12],
        }.ConnectionString;

        await using var factory = Factory("Production", connection);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"A normal start in Production did not migrate a new database. Body: {body}");
    }

    private static WebApplicationFactory<Program> Factory(string environment, string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Orders", connectionString);
        });

    private static async Task<JsonDocument> ReadDocumentAsync(CancellationToken ct)
    {
        if (s_document is null)
        {
            await using var factory = Factory("Development", AssemblyFixture.ConnectionString);
            using var client = factory.CreateClient();
            using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), ct);
            s_document = response.StatusCode == HttpStatusCode.OK ? await response.Content.ReadAsStringAsync(ct) : CommittedDocument();
        }

        Assert.IsNotNull(s_document, "No OpenAPI document: /openapi/v1.json gives no 200 in Development, and the repository has no committed document.");
        return JsonDocument.Parse(s_document);
    }

    private static string? CommittedDocument()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.slnx").Any() && !dir.EnumerateFiles("*.sln").Any())
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        var skip = new[] { "/bin/", "/obj/", "/TestResults/", "/node_modules/", "/.git/" };
        return dir.EnumerateFiles("*.json", SearchOption.AllDirectories)
            .Where(f => !skip.Any(s => f.FullName.Replace('\\', '/').Contains(s, StringComparison.Ordinal)))
            .Select(f => File.ReadAllText(f.FullName))
            .Where(t => Regex.IsMatch(t, "\"openapi\"\\s*:\\s*\"3\\.") && t.Contains("\"paths\"", StringComparison.Ordinal))
            .OrderByDescending(t => t.Length)
            .FirstOrDefault();
    }

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> Operations(JsonElement document)
    {
        foreach (var path in document.GetProperty("paths").EnumerateObject())
        {
            foreach (var op in path.Value.EnumerateObject())
            {
                if (op.Value.ValueKind == JsonValueKind.Object && op.Value.TryGetProperty("responses", out _))
                {
                    yield return (path.Name, op.Name.ToUpperInvariant(), op.Value);
                }
            }
        }
    }

    private static JsonElement Find(JsonElement document, string method, Regex path)
    {
        foreach (var p in document.GetProperty("paths").EnumerateObject())
        {
            if (path.IsMatch(p.Name) && p.Value.TryGetProperty(method, out var op))
            {
                return op;
            }
        }

        Assert.Fail($"The document has no {method.ToUpperInvariant()} {path}. Paths: " + string.Join(", ", document.GetProperty("paths").EnumerateObject().Select(p => p.Name)));
        return default;
    }

    private static bool HasProblemJson(JsonElement operation, string status) =>
        operation.TryGetProperty("responses", out var responses)
        && responses.TryGetProperty(status, out var response)
        && response.TryGetProperty("content", out var content)
        && content.TryGetProperty("application/problem+json", out _);

    private static void AssertProblem(JsonElement operation, string status) =>
        Assert.IsTrue(
            HasProblemJson(operation, status),
            $"Status {status} is not documented as application/problem+json. Responses: {(operation.TryGetProperty("responses", out var r) ? r.ToString() : "none")}");

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
