using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class AccountTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task SignIn_RedirectsToReturnUrl()
    {
        using var client = s_factory.CookieClient();
        using var response = await TestApi.CookieSignInAsync(client, TestApi.TomEmail, TestApi.TomPassword, "/account");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/account", response.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task SignIn_WrongPassword_Returns401()
    {
        using var client = s_factory.CookieClient();
        using var response = await TestApi.CookieSignInAsync(client, TestApi.TomEmail, "wrong", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ChangeEmail_FromAccountPage_ChangesIt()
    {
        var (api, email) = await s_factory.NewCustomerAsync("email");
        api.Dispose();
        using var client = s_factory.CookieClient();
        (await TestApi.CookieSignInAsync(client, email, TestApi.StrongPassword, "/account")).Dispose();
        var token = await TestApi.AntiforgeryTokenAsync(client, "/account");
        var newEmail = TestApi.NewEmail("changed");

        using var response = await client.PostAsync("/account/email", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["email"] = newEmail,
        }), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsTrue(await s_factory.QueryAsync(db => db.Users.AnyAsync(u => u.Email == newEmail)));
    }

    [TestMethod]
    public async Task StaffSignIn_RedirectsToLocalPage()
    {
        using var client = s_factory.CookieClient();
        using var response = await TestApi.CookieSignInAsync(client, TestApi.FinanceEmail, TestApi.FinancePassword, "/account", "/account/staff/login");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/account", response.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task StaffSignIn_Customer_IsRejected()
    {
        using var client = s_factory.CookieClient();
        using var response = await TestApi.CookieSignInAsync(client, TestApi.TomEmail, TestApi.TomPassword, "/account", "/account/staff/login");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Redirect_KnownTarget_Redirects()
    {
        using var client = s_factory.CookieClient();
        using var response = await client.GetAsync("/redirect?to=https://github.com/kiosk-shop", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("https://github.com/kiosk-shop", response.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task Redirect_UnknownTarget_IsRejected()
    {
        using var client = s_factory.CookieClient();
        using var response = await client.GetAsync("/redirect?to=https://unknown.example/", TestContext.CancellationToken);
        Assert.AreNotEqual(HttpStatusCode.Redirect, response.StatusCode);
    }
}
