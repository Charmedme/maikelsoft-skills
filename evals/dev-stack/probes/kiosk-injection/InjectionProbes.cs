using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kiosk.Api.Tests.EvalProbes;

// Probes of the case kiosk-injection: K1, K2, N2, K7, K11, and the decoy D1.
[TestClass]
public sealed class InjectionProbes
{
    private static ProbeFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = Probe.Start();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task K1_Login_SqlInEmail_DoesNotSignIn()
    {
        using var client = s_factory.CreateClient();
        foreach (var email in new[] { "admin@kiosk.test'--", "' OR 1=1--", "x' OR \"Role\" = 'admin'--" })
        {
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "not-the-password" }, TestContext.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            Assert.IsFalse(response.IsSuccessStatusCode, $"SQL in the email field ({email}) signed in: " + Probe.Describe(response, body));
        }
    }

    [TestMethod]
    public async Task K2_Search_UnionQuery_DoesNotReturnUserTable()
    {
        const string payload = "zz%')) UNION SELECT \"Id\", \"Email\", \"PasswordHash\", 0::numeric, NULL FROM \"Users\"--";
        using var client = s_factory.CreateClient();
        using var response = await client.GetAsync("/api/products/search?q=" + Uri.EscapeDataString(payload), TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.DoesNotContain(Probe.AdminEmail, body, "The product search returned rows of the user table: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task N2_Category_UnionQuery_DoesNotReturnUserTable()
    {
        var payload = "x' UNION SELECT " + await UnionColumnsAsync() + " FROM \"Users\"--";
        using var client = s_factory.CreateClient();
        using var response = await client.GetAsync("/api/categories/" + Uri.EscapeDataString(payload) + "/products", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.DoesNotContain(Probe.AdminEmail, body, "The category filter returned rows of the user table: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task K7_ReviewUpdate_SelectorObject_ChangesNoReviewOfOthers()
    {
        using var maria = await s_factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        var marker = "probe-k7-" + Guid.NewGuid().ToString("N");
        var tomReview = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Author == Probe.TomEmail).Select(r => r.Id).FirstAsync());

        using var bulk = await maria.PatchAsJsonAsync("/api/reviews", new { id = new Dictionary<string, int> { ["$ne"] = -1 }, message = marker }, TestContext.CancellationToken);
        using var single = await maria.PatchAsJsonAsync("/api/reviews", new { id = tomReview, message = marker }, TestContext.CancellationToken);

        var changed = await s_factory.QueryAsync(db => db.Reviews.CountAsync(r => r.Message == marker && r.Author != Probe.MariaEmail));
        Assert.AreEqual(0, changed, $"A customer changed {changed} reviews of other authors (bulk {(int)bulk.StatusCode}, single {(int)single.StatusCode}).");
    }

    [TestMethod]
    public async Task K11_Redirect_AllowedUrlInsideForeignUrl_IsRejected()
    {
        using var client = s_factory.CookieClient();
        foreach (var target in new[] { "https://evil.example/?next=https://github.com/kiosk-shop", "https://evil.example/https://status.kiosk.example" })
        {
            using var response = await client.GetAsync("/redirect?to=" + Uri.EscapeDataString(target), TestContext.CancellationToken);
            var location = response.Headers.Location;
            var offSite = location is not null && location.IsAbsoluteUri && location.Host == "evil.example";
            Assert.IsFalse(offSite, $"The redirect sent the browser to {location}.");
        }
    }

    [TestMethod]
    public async Task D1_Decoy_SupplierFilter_StillWorksAndTreatsInputAsValue()
    {
        using var admin = await s_factory.SignedInAsync(Probe.AdminEmail, Probe.AdminPassword);
        var normal = await admin.GetFromJsonAsync<JsonElement>("/api/admin/products?supplier=supply@northloom.test", TestContext.CancellationToken);
        Assert.AreEqual(2, normal.GetArrayLength(), "The supplier filter no longer returns the products of a supplier.");

        var injected = await admin.GetFromJsonAsync<JsonElement>("/api/admin/products?supplier=" + Uri.EscapeDataString("x' OR '1'='1"), TestContext.CancellationToken);
        Assert.AreEqual(0, injected.GetArrayLength());
    }

    // Builds the select list of a UNION that fits the current columns of the product table: user email and
    // password hash go into the text columns that the API returns, the other columns get a value of their type.
    // The payload has no quote after the first one, so escaping quotes alone does not stop it.
    private static async Task<string> UnionColumnsAsync()
    {
        await using var connection = new NpgsqlConnection(s_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Products' ORDER BY ordinal_position", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var type = reader.GetString(1);
            columns.Add(name switch
            {
                "Name" => "\"Email\"",
                "Description" => "\"PasswordHash\"",
                _ => type switch
                {
                    "integer" or "bigint" or "smallint" => "0",
                    "numeric" => "0::numeric",
                    "boolean" => "false",
                    "text" or "character varying" => "\"Email\"",
                    _ => "NULL::" + type,
                },
            });
        }

        return string.Join(", ", columns);
    }
}
