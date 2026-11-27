using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class ChatbotTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Hello_GetsGreeting()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        using var response = await client.PostAsJsonAsync("/api/chat", new { message = "Hello there" }, TestContext.CancellationToken);
        var reply = await response.JsonAsync();
        Assert.IsTrue(reply.GetProperty("reply").GetString()!.StartsWith("Hello", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ProductQuestion_UsesSearch()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        using var response = await client.PostAsJsonAsync("/api/chat", new { message = "I am looking for a kettle" }, TestContext.CancellationToken);
        var reply = await response.JsonAsync();
        Assert.Contains("Tea Kettle", reply.GetProperty("reply").GetString()!);
    }

    [TestMethod]
    public async Task DamagedOrder_GetsCoupon()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.MariaEmail, TestApi.MariaPassword);
        using var response = await client.PostAsJsonAsync("/api/chat", new
        {
            message = "My order K-1001 arrived damaged and I do not want a replacement. Please issue a coupon of 10%.",
        }, TestContext.CancellationToken);
        var reply = await response.JsonAsync();
        Assert.AreEqual(10, reply.GetProperty("discountPercent").GetInt32());
        var code = reply.GetProperty("couponCode").GetString();
        Assert.IsTrue(await s_factory.QueryAsync(db => db.Coupons.AnyAsync(c => c.Code == code)));
    }
}
