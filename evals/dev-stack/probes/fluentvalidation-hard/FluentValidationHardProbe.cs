using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for fluentvalidation-hard. POST /orders and the CSV import must give a 400 (or a rejection in the
// import report) with stable codes, never a 500. Most bodies here are not valid, so they write nothing.
// A "code" is a string in the body without white space that is not a message, a URL, the trace id, a default
// FluentValidation code (for example "NotEmptyValidator"), or "validation.failed".
// The null body test runs in Production: there minimal APIs run the endpoint filters with a null body.
[TestClass]
[DoNotParallelize]
public sealed class FluentValidationHardProbe
{
    private const string AdminKey = "probe-admin-key";
    private static readonly HashSet<string> s_standardKeys = new(StringComparer.OrdinalIgnoreCase) { "type", "title", "detail", "instance", "traceId" };

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CreateOrder_ValidOrder_Returns201()
    {
        var (status, body) = await PostOrderAsync(Order(Line("SKU-1", 1), Line("SKU-2", 2)));

        Assert.AreEqual(201, status, body);
    }

    [TestMethod]
    public async Task CreateOrder_EmptySku_HasOneErrorWithSkuCode()
    {
        var (status, body) = await PostOrderAsync(Order(Line("", 1)));

        Assert.AreEqual(400, status, body);
        var messages = ErrorsFor(body, @"^lines\[0\]\.sku$");
        Assert.AreEqual(1, messages, $"An empty SKU must give one error for Lines[0].Sku, not {messages}. Body: {body}");
        var codes = Codes(body);
        Assert.IsTrue(codes.Any(c => c.Contains("sku", StringComparison.OrdinalIgnoreCase)), $"No stable SKU code. Codes: [{string.Join(", ", codes)}]. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_UnknownSku_Returns400WithSkuCode()
    {
        var (status, body) = await PostOrderAsync(Order(Line("EVAL-UNKNOWN-7F3A", 1)));

        Assert.AreEqual(400, status, $"An unknown SKU must give 400. Body: {body}");
        Assert.IsTrue(Codes(body).Any(c => c.Contains("sku", StringComparison.OrdinalIgnoreCase)), $"No stable SKU code. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_InactiveSku_Returns400()
    {
        var (status, body) = await PostOrderAsync(Order(Line("SKU-OLD", 1)));

        Assert.AreEqual(400, status, $"A product that is not active must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_DuplicateSku_Returns400WithCode()
    {
        var (status, body) = await PostOrderAsync(Order(Line("SKU-1", 1), Line("SKU-1", 2)));

        Assert.AreEqual(400, status, $"The same SKU twice must give 400. Body: {body}");
        Assert.IsNotEmpty(Codes(body), $"The 400 for a duplicate SKU carries no stable code. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_DuplicateSkuOtherCase_Returns400()
    {
        var (status, body) = await PostOrderAsync(Order(Line("SKU-2", 1), Line(" sku-2", 2)));

        Assert.AreEqual(400, status, $"The catalog stores SKUs trimmed and in upper case, so \"SKU-2\" and \" sku-2\" are one product. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_NullLines_Returns400()
    {
        var (status, body) = await PostOrderAsync(new { customerId = Guid.CreateVersion7(), lines = (object?)null });

        Assert.AreEqual(400, status, $"\"lines\": null must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_NullSku_Returns400()
    {
        var (status, body) = await PostOrderAsync(Order(new { sku = (string?)null, quantity = 1, unitPrice = 1m }));

        Assert.AreEqual(400, status, $"A line with \"sku\": null must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_NullLine_Returns400()
    {
        var (status, body) = await PostOrderAsync(new { customerId = Guid.CreateVersion7(), lines = new object?[] { null } });

        Assert.AreEqual(400, status, $"\"lines\": [null] must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_NullBodyInProduction_Returns400()
    {
        await using var factory = Factory("Production");
        using var client = factory.CreateClient();
        var (status, body) = await SendAsync(client, "/orders", new StringContent("null", Encoding.UTF8, "application/json"), idempotency: true);

        Assert.AreEqual(400, status, $"A JSON null body must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateReturn_NullBodyInProduction_Returns400()
    {
        await using var factory = Factory("Production");
        using var client = factory.CreateClient();
        var (status, body) = await SendAsync(client, "/returns", new StringContent("null", Encoding.UTF8, "application/json"), idempotency: false);

        Assert.AreEqual(400, status, $"A JSON null body on POST /returns must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task Import_ValidOrder_IsImported()
    {
        var (status, body) = await ImportAsync($"E-{Guid.CreateVersion7():N};{Guid.CreateVersion7()};SKU-1;2;18.50");

        Assert.AreEqual(200, status, $"The import of a valid order failed. Body: {body}");
        using var document = JsonDocument.Parse(body);
        Assert.AreEqual(1, Property(document.RootElement, "imported").GetInt32(), body);
    }

    [TestMethod]
    public async Task Import_UnknownSku_IsRejectedWithSkuCode()
    {
        var (status, body) = await ImportAsync($"F-{Guid.CreateVersion7():N};{Guid.CreateVersion7()};EVAL-UNKNOWN-7F3A;1;1.00");

        Assert.AreEqual(200, status, $"The import must report an unknown SKU, not fail. Body: {body}");
        Assert.IsTrue(Codes(body).Any(c => c.Contains("sku", StringComparison.OrdinalIgnoreCase)), $"The import report has no SKU code. Body: {body}");
    }

    private static object Line(string sku, int quantity) => new { sku, quantity, unitPrice = 1m };

    private static object Order(params object[] lines) => new { customerId = Guid.CreateVersion7(), lines };

    private async Task<(int Status, string Body)> PostOrderAsync(object request)
    {
        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        return await SendAsync(client, "/orders", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), idempotency: true);
    }

    private async Task<(int Status, string Body)> ImportAsync(string row)
    {
        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);
        var csv = "externalRef;customerId;sku;quantity;unitPrice\n" + row + "\n";
        return await SendAsync(client, "/admin/orders/import", new StringContent(csv, Encoding.UTF8, "text/csv"), idempotency: false);
    }

    private async Task<(int Status, string Body)> SendAsync(HttpClient client, string url, HttpContent content, bool idempotency)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        if (idempotency)
        {
            message.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        }

        using var response = await client.SendAsync(message, TestContext.CancellationToken);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private static WebApplicationFactory<Program> Factory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Admin:ApiKey", AdminKey);
        });

    private static JsonElement Property(JsonElement element, string name) =>
        element.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    // The number of messages under the "errors" key that matches the pattern (case-insensitive). -1 when there is no such key.
    private static int ErrorsFor(string body, string pattern)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var errors = document.RootElement.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, "errors", StringComparison.OrdinalIgnoreCase));
            if (errors.Value.ValueKind != JsonValueKind.Object)
            {
                return -1;
            }

            var field = errors.Value.EnumerateObject().FirstOrDefault(p => Regex.IsMatch(p.Name, pattern, RegexOptions.IgnoreCase));
            return field.Value.ValueKind == JsonValueKind.Array ? field.Value.GetArrayLength() : field.Value.ValueKind == JsonValueKind.Undefined ? -1 : 1;
        }
        catch (JsonException)
        {
            return -1;
        }
    }

    private static List<string> Codes(string body)
    {
        var codes = new List<string>();
        try
        {
            using var document = JsonDocument.Parse(body);
            Collect(document.RootElement, codes);
        }
        catch (JsonException)
        {
        }

        return codes;
    }

    private static void Collect(JsonElement element, List<string> codes)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject().Where(p => !s_standardKeys.Contains(p.Name)))
                {
                    Collect(property.Value, codes);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, codes);
                }

                break;
            case JsonValueKind.String:
                var value = element.GetString() ?? "";
                if (value.Length is > 0 and <= 80
                    && !value.Any(char.IsWhiteSpace)
                    && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !value.EndsWith("Validator", StringComparison.Ordinal)
                    && !value.StartsWith("F-", StringComparison.Ordinal)
                    && !value.Equals("validation.failed", StringComparison.OrdinalIgnoreCase))
                {
                    codes.Add(value);
                }

                break;
        }
    }
}
