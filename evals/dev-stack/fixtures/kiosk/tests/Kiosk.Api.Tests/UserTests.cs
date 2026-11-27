using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class UserTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Register_ThenLogin_ReturnsToken()
    {
        var (client, email) = await s_factory.NewCustomerAsync("register");
        using (client)
        {
            var me = await client.GetFromJsonAsync<JsonElement>("/api/users/me", TestContext.CancellationToken);
            Assert.AreEqual(email, me.GetProperty("email").GetString());
        }
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = s_factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestApi.MariaEmail, password = "wrong" }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_ExistingEmail_Returns409()
    {
        using var client = s_factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/users", new { email = TestApi.TomEmail, password = TestApi.StrongPassword }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Patch_DisplayName_ChangesIt()
    {
        var (client, email) = await s_factory.NewCustomerAsync("patch");
        using (client)
        {
            using var content = new StringContent("[{\"op\":\"replace\",\"path\":\"/displayName\",\"value\":\"Robin\"}]", Encoding.UTF8, "application/json-patch+json");
            using var response = await client.PatchAsync("/api/users/me", content, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
            var name = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.DisplayName).SingleAsync());
            Assert.AreEqual("Robin", name);
        }
    }

    [TestMethod]
    public async Task Profile_UpdatesDisplayName()
    {
        var (client, _) = await s_factory.NewCustomerAsync("profile");
        using (client)
        {
            using var response = await client.PutAsJsonAsync("/api/users/me/profile", new { displayName = "Kim" }, TestContext.CancellationToken);
            var user = await response.JsonAsync();
            Assert.AreEqual("Kim", user.GetProperty("displayName").GetString());
        }
    }

    [TestMethod]
    public async Task ProfileImage_FromImageHost_IsStored()
    {
        var (client, email) = await s_factory.NewCustomerAsync("avatar");
        using (client)
        {
            using var response = await client.PutAsJsonAsync("/api/users/me/profile-image", new { url = "https://cdn.kiosk.example/avatars/robin.png" }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
            var image = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.ProfileImage).SingleAsync());
            Assert.IsNotNull(image);
        }
    }

    [TestMethod]
    public async Task SecurityQuestions_AreListed()
    {
        using var client = s_factory.CreateClient();
        var questions = await client.GetFromJsonAsync<JsonElement>("/api/users/security-questions", TestContext.CancellationToken);
        Assert.AreEqual(3, questions.GetArrayLength());
    }
}
