using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Api.Tests.EvalProbes;

// Probes of the case kiosk-platform, part 1: N7 to N14, and the decoys D5 and D6.
[TestClass]
public sealed class PlatformProbes
{
    private static ProbeFactory s_factory = null!;
    private static readonly List<Uri> s_outbound = [];

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = Probe.Start(() => new ProbeFactory
    {
        ExtraServices = services => services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => new RecordingHandler(s_outbound))),
    });

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task N7_SignIn_ReturnUrlToOtherHost_StaysLocal()
    {
        foreach (var returnUrl in new[] { "//evil.example/account", "/\\evil.example/account", "/\\/evil.example" })
        {
            using var client = s_factory.CookieClient();
            using var response = await Probe.CookieSignInAsync(client, Probe.MariaEmail, Probe.MariaPassword, returnUrl);
            var location = response.Headers.Location?.OriginalString ?? string.Empty;
            var offSite = location.StartsWith("//", StringComparison.Ordinal) || location.StartsWith("/\\", StringComparison.Ordinal)
                || (Uri.TryCreate(location, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https" && absolute.Host != "localhost");
            Assert.IsFalse(offSite, $"Sign-in with returnUrl {returnUrl} redirected to {location}.");
        }
    }

    [TestMethod]
    public async Task N8_InvoiceDownload_PathOutsideInvoiceFolder_IsRejected()
    {
        var folder = Directory.CreateTempSubdirectory("kiosk-probe-n8-");
        try
        {
            var secret = Path.Combine(folder.FullName, "secret.pdf");
            await File.WriteAllTextAsync(secret, "probe-n8-secret-content", TestContext.CancellationToken);
            using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
            using var response = await tom.GetAsync("/api/invoices/download?name=" + Uri.EscapeDataString(secret), TestContext.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            Assert.DoesNotContain("probe-n8-secret-content", body, "The invoice download returned a file outside the invoice folder.");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task N9_Upload_ActiveContentWithImageType_IsNotServedAsActiveContent()
    {
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        foreach (var (extension, payload) in new[]
        {
            (".html", "<html><body><script>alert('probe-n9')</script></body></html>"),
            (".svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert('probe-n9')</script></svg>"),
        })
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(payload));
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(file, "file", $"probe-{Guid.NewGuid():N}{extension}");
            using var upload = await tom.PostAsync("/api/uploads", form, TestContext.CancellationToken);
            if (!upload.IsSuccessStatusCode)
            {
                continue;
            }

            var url = upload.Headers.Location?.OriginalString;
            if (url is null || url.Length == 0)
            {
                var body = await upload.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(TestContext.CancellationToken);
                url = body.GetProperty("url").GetString()!;
            }

            using var served = await s_factory.CreateClient().GetAsync(url, TestContext.CancellationToken);
            var type = served.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var active = served.IsSuccessStatusCode && (type.Contains("html", StringComparison.OrdinalIgnoreCase) || type.Contains("svg", StringComparison.OrdinalIgnoreCase) || type.Contains("xml", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(active, $"The uploaded {extension} file is served as {type} from {url}.");
        }
    }

    [TestMethod]
    public async Task N10_XmlOrder_ExternalEntity_IsNotResolved()
    {
        var folder = Directory.CreateTempSubdirectory("kiosk-probe-n10-");
        try
        {
            var secret = Path.Combine(folder.FullName, "secret.txt");
            await File.WriteAllTextAsync(secret, "probe-n10-secret-content", TestContext.CancellationToken);
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <!DOCTYPE order [ <!ENTITY data SYSTEM "{new Uri(secret).AbsoluteUri}"> ]>
                <order reference="PO-N10">
                  <deliveryAddress>Retail Park 5, Breda</deliveryAddress>
                  <note>&data;</note>
                  <line sku="NTB-400" quantity="1" />
                </order>
                """;
            using var partner = await s_factory.SignedInAsync(Probe.PartnerEmail, Probe.PartnerPassword);
            using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            using var response = await partner.PostAsync("/api/b2b/orders", content, TestContext.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            var stored = await s_factory.QueryAsync(db => db.Orders.AnyAsync(o => o.Note != null && o.Note.Contains("probe-n10-secret-content")));
            Assert.DoesNotContain("probe-n10-secret-content", body, "The XML import read a local file through an external entity.");
            Assert.IsFalse(stored, "The XML import stored the content of a local file.");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task N11_JsonOrder_TypeName_DoesNotCreateAppType()
    {
        var target = Path.Combine(Path.GetTempPath(), "kiosk-probe-n11-" + Guid.NewGuid().ToString("N"));
        var json = "{\"reference\":\"PO-N11\",\"deliveryAddress\":\"Retail Park 5, Breda\",\"lines\":[{\"sku\":\"NTB-400\",\"quantity\":1}],"
            + "\"extensions\":{\"$type\":\"Kiosk.Api.Admin.ExportSchedule, Kiosk.Api\",\"name\":\"probe\",\"archivePath\":"
            + System.Text.Json.JsonSerializer.Serialize(target) + "}}";
        try
        {
            using var partner = await s_factory.SignedInAsync(Probe.PartnerEmail, Probe.PartnerPassword);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await partner.PostAsync("/api/b2b/orders/json", content, TestContext.CancellationToken);
            Assert.IsFalse(Directory.Exists(target), $"A $type in the partner JSON ran code of an app class (status {(int)response.StatusCode}).");
        }
        finally
        {
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task N12_ProfileImage_InternalAddress_IsNotFetched()
    {
        var email = await s_factory.RegisterAsync("n12");
        using var client = await s_factory.SignedInAsync(email, Probe.StrongPassword);
        var internalUrls = new[]
        {
            "http://169.254.169.254/latest/meta-data/iam/security-credentials/",
            "http://10.0.0.5/internal/admin/config",
            "http://127.0.0.1:5180/metrics",
            "http://localhost/api/metrics/summary",
            "http://[::1]/metrics",
        };
        foreach (var url in internalUrls)
        {
            (await client.PutAsJsonAsync("/api/users/me/profile-image", new { url }, TestContext.CancellationToken)).Dispose();
        }

        List<Uri> seen;
        lock (s_outbound)
        {
            seen = s_outbound.Where(u => u.IsLoopback || u.Host.StartsWith("10.", StringComparison.Ordinal) || u.Host.StartsWith("169.254.", StringComparison.Ordinal)).ToList();
        }

        Assert.IsEmpty(seen, "The server fetched internal addresses: " + string.Join(", ", seen));
    }

    [TestMethod]
    public async Task N13_ChangeEmail_CrossSiteFormWithoutToken_IsRejected()
    {
        var email = await s_factory.RegisterAsync("n13");
        using var victim = s_factory.CookieClient();
        (await Probe.CookieSignInAsync(victim, email, Probe.StrongPassword, "/account")).Dispose();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/account/email")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = Probe.NewEmail("attacker") }),
        };
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Referrer = new Uri("https://evil.example/win-a-prize");
        using var response = await victim.SendAsync(request, TestContext.CancellationToken);

        var unchanged = await s_factory.QueryAsync(db => db.Users.AnyAsync(u => u.Email == email));
        Assert.IsTrue(unchanged, $"A cross-site form without an antiforgery token changed the email (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task N13_SessionCookie_IsNotSentCrossSite()
    {
        using var client = s_factory.CookieClient();
        using var response = await Probe.CookieSignInAsync(client, Probe.TomEmail, Probe.TomPassword, "/account");
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.FirstOrDefault(v => v.StartsWith("kiosk.session", StringComparison.Ordinal)) : null;
        Assert.IsNotNull(cookie, "The sign-in set no session cookie.");
        Assert.DoesNotContain("samesite=none", cookie, StringComparison.OrdinalIgnoreCase, "The session cookie goes with cross-site requests: " + cookie);
    }

    [TestMethod]
    public async Task N14_Cors_ForeignOrigin_GetsNoCredentialedAccess()
    {
        using var client = s_factory.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/users/me");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization");
        using var preflightResponse = await client.SendAsync(preflight, TestContext.CancellationToken);

        using var simple = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        simple.Headers.Add("Origin", "https://evil.example");
        using var simpleResponse = await client.SendAsync(simple, TestContext.CancellationToken);

        foreach (var response in new[] { preflightResponse, simpleResponse })
        {
            var origin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var o) ? o.First() : null;
            Assert.IsFalse(origin is "https://evil.example" or "*" && response.Headers.Contains("Access-Control-Allow-Credentials"),
                $"CORS allows the origin https://evil.example with credentials (Access-Control-Allow-Origin: {origin}).");
            Assert.AreNotEqual("https://evil.example", origin, "CORS reflects a foreign origin.");
        }
    }

    [TestMethod]
    public async Task D5_Decoy_StaffSignIn_KeepsLocalRedirect()
    {
        using var local = s_factory.CookieClient();
        using var localResponse = await Probe.CookieSignInAsync(local, Probe.FinanceEmail, Probe.FinancePassword, "/account", "/account/staff/login");
        Assert.AreEqual(HttpStatusCode.Redirect, localResponse.StatusCode);
        Assert.AreEqual("/account", localResponse.Headers.Location?.OriginalString);

        using var foreign = s_factory.CookieClient();
        using var foreignResponse = await Probe.CookieSignInAsync(foreign, Probe.FinanceEmail, Probe.FinancePassword, "//evil.example/", "/account/staff/login");
        var location = foreignResponse.Headers.Location?.OriginalString ?? string.Empty;
        Assert.IsFalse(location.Contains("evil.example", StringComparison.OrdinalIgnoreCase), "The staff sign-in redirected to another host.");
    }

    [TestMethod]
    public async Task D6_Decoy_PriceList_StillRejectsDocumentTypes()
    {
        using var partner = await s_factory.SignedInAsync(Probe.PartnerEmail, Probe.PartnerPassword);
        var folder = Directory.CreateTempSubdirectory("kiosk-probe-d6-");
        try
        {
            var secret = Path.Combine(folder.FullName, "secret.txt");
            await File.WriteAllTextAsync(secret, "probe-d6-secret", TestContext.CancellationToken);
            var xml = $"""<?xml version="1.0"?><!DOCTYPE prices [ <!ENTITY data SYSTEM "{new Uri(secret).AbsoluteUri}"> ]><prices><price sku="&data;" amount="1.00" /></prices>""";
            using var attack = await partner.PostAsync("/api/b2b/price-list", new StringContent(xml, Encoding.UTF8, "application/xml"), TestContext.CancellationToken);
            Assert.DoesNotContain("probe-d6-secret", await attack.Content.ReadAsStringAsync(TestContext.CancellationToken));
        }
        finally
        {
            folder.Delete(recursive: true);
        }

        using var valid = await partner.PostAsync("/api/b2b/price-list", new StringContent("""<prices><price sku="PEN-410" amount="35.50" /></prices>""", Encoding.UTF8, "application/xml"), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, valid.StatusCode, "The price list import no longer accepts a valid file.");
    }

    private sealed class RecordingHandler(List<Uri> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (requests)
            {
                requests.Add(request.RequestUri!);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("kiosk-test-only-internal-config") };
            return Task.FromResult(response);
        }
    }
}
