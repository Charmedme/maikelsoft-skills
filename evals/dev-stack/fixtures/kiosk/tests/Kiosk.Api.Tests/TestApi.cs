using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kiosk.Api.Tests;

public static partial class TestApi
{
    public const string AdminEmail = "admin@kiosk.test";
    public const string AdminPassword = "Kiosk-Admin-2026!";
    public const string MariaEmail = "maria.lopez@kiosk.test";
    public const string MariaPassword = "Maria-Spring-2026!";
    public const string TomEmail = "tom.berg@kiosk.test";
    public const string TomPassword = "Tom-Garden-2026!";
    public const string FinanceEmail = "finance@kiosk.test";
    public const string FinancePassword = "Finance-Ledger-2026!";
    public const string PartnerEmail = "orders@partner-retail.test";
    public const string PartnerPassword = "Partner-Feed-2026!";
    public const string StrongPassword = "Correct-Horse-Battery-42!";

    public static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.test";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Login of {email} failed.");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    public static async Task<HttpClient> SignedInClientAsync(this KioskApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, password));
        return client;
    }

    public static async Task<(HttpClient Client, string Email)> NewCustomerAsync(this KioskApiFactory factory, string prefix = "customer")
    {
        var email = NewEmail(prefix);
        using var anonymous = factory.CreateClient();
        using var response = await anonymous.PostAsJsonAsync("/api/users", new { email, password = StrongPassword, displayName = "Test", securityQuestionId = 1, securityAnswer = "Rex" });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        return (await factory.SignedInClientAsync(email, StrongPassword), email);
    }

    public static HttpClient CookieClient(this KioskApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<string> AntiforgeryTokenAsync(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var match = TokenField().Match(html);
        Assert.IsTrue(match.Success, $"No antiforgery token on {page}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> CookieSignInAsync(HttpClient client, string email, string password, string? returnUrl, string path = "/account/login")
    {
        var token = await AntiforgeryTokenAsync(client, path);
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = email,
            ["password"] = password,
            ["returnUrl"] = returnUrl ?? string.Empty,
        };
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response)
    {
        Assert.IsTrue(response.IsSuccessStatusCode, $"Status {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex TokenField();
}
