using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class PasswordResetTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task SecurityAnswer_ResetsPassword()
    {
        var (client, email) = await s_factory.NewCustomerAsync("answer");
        client.Dispose();
        using var anonymous = s_factory.CreateClient();
        const string newPassword = "Another-Long-Passphrase-77!";
        using var reset = await anonymous.PostAsJsonAsync("/api/users/reset-password", new { email, answer = "rex", newPassword }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.IsFalse(string.IsNullOrEmpty(await TestApi.LoginAsync(anonymous, email, newPassword)));
    }

    [TestMethod]
    public async Task ForgotPassword_MailLink_ResetsPassword()
    {
        var (client, email) = await s_factory.NewCustomerAsync("forgot");
        client.Dispose();
        using var anonymous = s_factory.CreateClient();
        using var forgot = await anonymous.PostAsJsonAsync("/api/users/forgot-password", new { email }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Accepted, forgot.StatusCode);

        var body = await s_factory.QueryAsync(db => db.OutboxEmails.Where(m => m.To == email).Select(m => m.Body).SingleAsync());
        var token = Regex.Match(body, "token=([A-Za-z0-9_-]+)").Groups[1].Value;
        Assert.IsFalse(string.IsNullOrEmpty(token), body);

        const string newPassword = "Fresh-Long-Passphrase-88!";
        using var confirm = await anonymous.PostAsJsonAsync("/api/users/reset-password/confirm", new { email, token, newPassword }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.IsFalse(string.IsNullOrEmpty(await TestApi.LoginAsync(anonymous, email, newPassword)));
    }

    [TestMethod]
    public async Task Confirm_WrongToken_Returns400()
    {
        using var anonymous = s_factory.CreateClient();
        using var confirm = await anonymous.PostAsJsonAsync("/api/users/reset-password/confirm", new { email = TestApi.TomEmail, token = "000000000", newPassword = "Whatever-Long-Passphrase-1!" }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, confirm.StatusCode);
    }
}
