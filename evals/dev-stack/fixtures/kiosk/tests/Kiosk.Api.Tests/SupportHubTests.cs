using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class SupportHubTests
{
    private static KioskApiFactory s_factory = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Customer_ReceivesOwnMessage()
    {
        using var api = s_factory.CreateClient();
        var token = await TestApi.LoginAsync(api, TestApi.TomEmail, TestApi.TomPassword);
        var userId = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == TestApi.TomEmail).Select(u => u.Id).SingleAsync());
        var received = new ConcurrentQueue<string>();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(s_factory.Server.BaseAddress, "/hubs/support"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => s_factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
        connection.On<JsonElement>("ReceiveMessage", m => received.Enqueue(m.GetProperty("text").GetString()!));
        await connection.StartAsync();

        await connection.InvokeAsync("JoinConversation", $"customer-{userId}");
        await connection.InvokeAsync("SendMessage", $"customer-{userId}", "Where is my parcel?");

        for (var i = 0; i < 50 && received.IsEmpty; i++)
        {
            await Task.Delay(100);
        }

        CollectionAssert.Contains(received.ToList(), "Where is my parcel?");
    }
}
