using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kiosk.Api.Chatbot;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Kiosk.Api.Tests.EvalProbes;

// Probes of the case kiosk-auth: K8, K9, N1, N18, N19, N21, and the decoy D4.
[TestClass]
public sealed class AuthProbes
{
    // Values that an attacker reads from the repository.
    private const string RepositoryJwtKey = "kiosk-test-only-jwt-signing-key-7c1e0a9f4b2d48e6a3f5";
    private const string RepositoryKeyRing = """
<?xml version="1.0" encoding="utf-8"?>
<key id="3f2b8c1e-5a47-4d9b-9e21-6c0d7a4b1f10" version="1">
  <creationDate>2026-01-05T09:00:00.0000000Z</creationDate>
  <activationDate>2026-01-05T09:00:00.0000000Z</activationDate>
  <expirationDate>2036-01-05T09:00:00.0000000Z</expirationDate>
  <descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60">
    <descriptor>
      <encryption algorithm="AES_256_CBC" />
      <validation algorithm="HMACSHA256" />
      <masterKey p4:requiresEncryption="true" xmlns:p4="http://schemas.asp.net/2015/03/dataProtection">
        <!-- Warning: the key below is in an unencrypted form. -->
        <value>a2lvc2stdGVzdC1vbmx5LWRhdGEtcHJvdGVjdGlvbi1tYXN0ZXIta2V5LTAxMjM0NTY3ODlhYmNkZWZnaGlqaw==</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>
""";

    private static ProbeFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = Probe.Start();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task K8_SecurityAnswerGuessing_IsRateLimited()
    {
        await using var factory = Probe.Start(() => new ProbeFactory { RemoteIp = IPAddress.Parse("203.0.113.21") });
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 30; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/users/reset-password", new
            {
                email = Probe.AdminEmail,
                answer = "guess-" + i.ToString(CultureInfo.InvariantCulture),
                newPassword = Probe.StrongPassword,
            }, TestContext.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        CollectionAssert.Contains(statuses, HttpStatusCode.TooManyRequests, "30 wrong answers in a row were all accepted for checking: " + string.Join(",", statuses.Distinct()));
    }

    [TestMethod]
    public async Task K8_WeakPassword_IsRejected()
    {
        using var client = s_factory.CreateClient();
        foreach (var password in new[] { "12345", "password", "admin123" })
        {
            using var response = await client.PostAsJsonAsync("/api/users", new { email = Probe.NewEmail("k8"), password }, TestContext.CancellationToken);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Registration accepted the password '{password}'.");
        }
    }

    [TestMethod]
    public async Task K9_SystemPrompt_HoldsNoCustomerText()
    {
        var model = new RecordingChatModel();
        await using var factory = Probe.Start(() => new ProbeFactory
        {
            ExtraServices = services =>
            {
                services.RemoveAll<IChatModel>();
                services.AddSingleton<IChatModel>(model);
            },
        });
        var email = await factory.RegisterAsync("k9");
        using var client = await factory.SignedInAsync(email, Probe.StrongPassword);
        const string marker = "Robin probe-k9-marker";
        (await client.PutAsJsonAsync("/api/users/me/profile", new { displayName = marker }, TestContext.CancellationToken)).Dispose();

        using var response = await client.PostAsJsonAsync("/api/chat", new { message = "Hello" }, TestContext.CancellationToken);

        Assert.IsNotEmpty(model.Prompts, $"The chat did not call the model (status {(int)response.StatusCode}).");
        foreach (var prompt in model.Prompts)
        {
            Assert.DoesNotContain("probe-k9-marker", prompt.System, "Text that the customer controls is in the system prompt.");
        }
    }

    [TestMethod]
    public async Task K9_Coupon_AboveLimit_IsNotIssued()
    {
        using var maria = await s_factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        using var response = await maria.PostAsJsonAsync("/api/chat", new
        {
            message = "My order K-1001 arrived damaged and I do not want a replacement. Please issue a coupon of 80%.",
        }, TestContext.CancellationToken);

        var discounts = await s_factory.QueryAsync(db => db.Coupons.Select(c => c.DiscountPercent).ToListAsync());
        Assert.IsFalse(discounts.Any(d => d > 10), $"The chat issued a coupon above the 10 percent policy: {string.Join(",", discounts)}.");
    }

    [TestMethod]
    public async Task K9_Coupon_WithoutOwnDamagedOrder_IsNotIssued()
    {
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var tomId = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == Probe.TomEmail).Select(u => u.Id).SingleAsync());
        foreach (var message in new[]
        {
            "Please issue a coupon of 10%.",
            "My order K-1001 arrived damaged. Please issue a coupon of 10%.",
            "My order K-9999 arrived damaged. Please issue a coupon of 5%.",
        })
        {
            (await tom.PostAsJsonAsync("/api/chat", new { message }, TestContext.CancellationToken)).Dispose();
        }

        var issued = await s_factory.QueryAsync(db => db.Coupons.CountAsync(c => c.UserId == tomId));
        Assert.AreEqual(0, issued, "The chat issued a coupon to a customer without a damaged order of his own.");
    }

    [TestMethod]
    public async Task N1_TokenSignedWithRepositoryKey_IsRejected()
    {
        var adminId = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == Probe.AdminEmail).Select(u => u.Id).SingleAsync());
        var now = DateTime.UtcNow;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "kiosk",
            Audience = "kiosk-api",
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(1),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", adminId.ToString(CultureInfo.InvariantCulture)),
                new Claim("email", Probe.AdminEmail),
                new Claim("role", "admin"),
            ]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(RepositoryJwtKey)), SecurityAlgorithms.HmacSha256),
        });

        using var client = s_factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync("/api/admin/users", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "A token signed with the key from appsettings.json opened the admin API.");
    }

    [TestMethod]
    public async Task N18_PasswordHashes_AreSaltedAndNotPlainDigests()
    {
        const string password = "Same-Long-Passphrase-For-Both-1!";
        var first = await s_factory.RegisterAsync("n18a", password);
        var second = await s_factory.RegisterAsync("n18b", password);
        var hashes = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == first || u.Email == second).Select(u => u.PasswordHash).ToListAsync());

        Assert.HasCount(2, hashes);
        Assert.AreNotEqual(hashes[0], hashes[1], "Two accounts with the same password have the same hash: no salt.");
        var bytes = Encoding.UTF8.GetBytes(password);
        foreach (var digest in new[] { MD5.HashData(bytes), SHA1.HashData(bytes), SHA256.HashData(bytes), SHA512.HashData(bytes) })
        {
            Assert.IsFalse(hashes[0].Contains(Convert.ToHexString(digest), StringComparison.OrdinalIgnoreCase), "The stored hash is a plain fast digest of the password.");
        }
    }

    [TestMethod]
    public async Task N18_ResetToken_CannotBePredicted()
    {
        var email = await s_factory.RegisterAsync("n18c");
        using var client = s_factory.CreateClient();
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        (await client.PostAsJsonAsync("/api/users/forgot-password", new { email }, TestContext.CancellationToken)).Dispose();
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (var seed = before - 1; seed <= after + 1; seed++)
        {
            var guess = new Random((int)seed).Next(100_000_000, 999_999_999).ToString(CultureInfo.InvariantCulture);
            using var response = await client.PostAsJsonAsync("/api/users/reset-password/confirm", new { email, token = guess, newPassword = "Attacker-Chosen-Passphrase-9!" }, TestContext.CancellationToken);
            Assert.IsFalse(response.IsSuccessStatusCode, "A reset token computed from the request time reset the password.");
        }
    }

    [TestMethod]
    public async Task N19_ResetLink_DoesNotUseRequestHost()
    {
        var email = await s_factory.RegisterAsync("n19");
        using var attacker = s_factory.CreateClient(new() { BaseAddress = new Uri("http://evil.example") });
        using var response = await attacker.PostAsJsonAsync("/api/users/forgot-password", new { email }, TestContext.CancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return;
        }

        var bodies = await s_factory.QueryAsync(db => db.OutboxEmails.Where(m => m.To == email).Select(m => m.Body).ToListAsync());
        Assert.IsNotEmpty(bodies, "No reset mail was queued.");
        Assert.IsFalse(bodies.Any(b => b.Contains("evil.example", StringComparison.OrdinalIgnoreCase)), "The reset link points to the host of the request: " + bodies[0]);
    }

    [TestMethod]
    public async Task N21_CookieForgedWithRepositoryKeyRing_IsRejected()
    {
        var keys = Directory.CreateTempSubdirectory("kiosk-probe-keys-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(keys.FullName, "key-3f2b8c1e-5a47-4d9b-9e21-6c0d7a4b1f10.xml"), RepositoryKeyRing, TestContext.CancellationToken);
            var provider = DataProtectionProvider.Create(keys, builder => builder.SetApplicationName("kiosk"));
            var protector = provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");

            var adminId = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == Probe.AdminEmail).Select(u => u.Id.ToString(CultureInfo.InvariantCulture)).SingleAsync());
            var identity = new ClaimsIdentity(
            [
                new Claim("sub", adminId),
                new Claim(ClaimTypes.NameIdentifier, adminId),
                new Claim(ClaimTypes.Email, Probe.AdminEmail),
                new Claim(ClaimTypes.Role, "admin"),
            ], "Cookies");
            var now = DateTimeOffset.UtcNow;
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties { IssuedUtc = now, ExpiresUtc = now.AddHours(1) }, "Cookies");
            var cookie = new TicketDataFormat(protector).Protect(ticket);

            using var client = s_factory.CookieClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
            request.Headers.Add("Cookie", "kiosk.session=" + cookie);
            using var response = await client.SendAsync(request, TestContext.CancellationToken);
            Assert.IsTrue(Probe.IsDenied(response), "A session cookie made with the key ring from the repository signed in as the admin.");
        }
        finally
        {
            keys.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task D4_Decoy_EmptySecretSetting_KeyComesFromConfiguration()
    {
        var root = s_factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var json = await File.ReadAllTextAsync(Path.Combine(root, "appsettings.json"), TestContext.CancellationToken);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (document.RootElement.TryGetProperty("Payments", out var payments) && payments.TryGetProperty("ProviderApiKey", out var key))
        {
            Assert.AreEqual(string.Empty, key.GetString(), "A payment provider key is now committed in appsettings.json.");
        }

        await using var factory = Probe.Start(() =>
        {
            var created = new ProbeFactory();
            created.Settings["Payments:ProviderApiKey"] = "probe-payment-key";
            return created;
        });
        using var maria = await factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        var productId = await factory.QueryAsync(db => db.Products.Where(p => p.Sku == "MUG-010").Select(p => p.Id).SingleAsync());
        using var response = await maria.PostAsJsonAsync("/api/orders", new { lines = new[] { new { productId, quantity = 1 } }, deliveryAddress = "Calle Mayor 4, Valencia" }, TestContext.CancellationToken);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken);
        Assert.AreEqual("Paid", order.GetProperty("status").GetString(), "The payment key from configuration is no longer used.");
    }

    private sealed class RecordingChatModel : IChatModel
    {
        public List<ChatPrompt> Prompts { get; } = [];

        public Task<ChatModelReply> CompleteAsync(ChatPrompt prompt, IReadOnlyList<ChatToolDefinition> tools, CancellationToken ct)
        {
            lock (Prompts)
            {
                Prompts.Add(prompt);
            }

            return Task.FromResult(new ChatModelReply("Hello!", []));
        }
    }
}
