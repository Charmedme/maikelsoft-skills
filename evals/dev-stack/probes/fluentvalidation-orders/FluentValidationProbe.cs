using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for fluentvalidation-trap. Each request fails validation, so no test writes to the database.
// The null body test runs in Production: there minimal APIs run the endpoint filters with a null body.
// In Development, ThrowOnBadRequest throws before the filters, so that test would give 500 for every solution.
// A "code" is a string in the response body without white space that is not a message, a URL, the
// trace id, the default FluentValidation code (for example "NotEmptyValidator"), or "validation.failed".
[TestClass]
[DoNotParallelize]
public sealed class FluentValidationProbe
{
    private static readonly HashSet<string> s_standardKeys = new(StringComparer.OrdinalIgnoreCase) { "type", "title", "detail", "instance", "traceId" };

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CreateOrder_EmptySku_ReturnsValidatorCode()
    {
        var (status, body) = await PostAsync(Order(("", 1)));

        Assert.AreEqual(400, status, body);
        var codes = Codes(body);
        Assert.IsTrue(
            codes.Any(code => code.Contains("sku", StringComparison.OrdinalIgnoreCase)),
            $"The 400 for an empty SKU has no stable code for the SKU rule. Codes: [{string.Join(", ", codes)}]. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_UnknownSku_Returns400NotServerError()
    {
        var (status, body) = await PostAsync(Order(("EVAL-UNKNOWN-7F3A", 1)));

        Assert.AreEqual(400, status, $"An unknown SKU must give 400. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_DuplicateSku_ResponseCarriesCode()
    {
        var (status, body) = await PostAsync(Order(("SKU-1", 1), ("SKU-1", 2)));

        Assert.AreEqual(400, status, $"The same SKU twice must give 400. Body: {body}");
        var codes = Codes(body);
        Assert.IsTrue(codes.Count > 0, $"The 400 for a duplicate SKU carries no stable code. Body: {body}");
    }

    [TestMethod]
    public async Task CreateOrder_NullBodyInProduction_Returns400()
    {
        await using var factory = Factory("Production");
        using var client = factory.CreateClient();
        using var content = new StringContent("null", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/orders", UriKind.Relative), content, TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(400, (int)response.StatusCode, $"A JSON null body must give 400. Body: {body}");
    }

    private static object Order(params (string Sku, int Quantity)[] lines) =>
        new { customerId = Guid.CreateVersion7(), lines = lines.Select(l => new { sku = l.Sku, quantity = l.Quantity, unitPrice = 1m }).ToArray() };

    private async Task<(int Status, string Body)> PostAsync(object request)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/orders", UriKind.Relative), content, TestContext.CancellationToken);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private static WebApplicationFactory<Program> Factory(string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        });

    private static List<string> Codes(string body)
    {
        var codes = new List<string>();
        try
        {
            using var document = JsonDocument.Parse(body);
            Collect(document.RootElement, null, codes);
        }
        catch (JsonException)
        {
        }

        return codes;
    }

    private static void Collect(JsonElement element, string? key, List<string> codes)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (!s_standardKeys.Contains(property.Name))
                    {
                        Collect(property.Value, property.Name, codes);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, key, codes);
                }

                break;
            case JsonValueKind.String:
                var value = element.GetString() ?? "";
                if (value.Length is > 0 and <= 80
                    && !value.Any(char.IsWhiteSpace)
                    && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !value.EndsWith("Validator", StringComparison.Ordinal)
                    && !value.Equals("validation.failed", StringComparison.OrdinalIgnoreCase))
                {
                    codes.Add(value);
                }

                break;
        }
    }
}
