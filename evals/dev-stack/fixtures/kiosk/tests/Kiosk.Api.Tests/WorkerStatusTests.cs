using System.Net;
using Kiosk.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class WorkerStatusTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Status_IsOk()
    {
        await using var factory = new WebApplicationFactory<WorkerApp>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Kiosk", AssemblyFixture.ConnectionString);
            builder.UseSetting("Outbox:Enabled", "false");
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/status", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Jobs_FromOutside_AreForbidden()
    {
        await using var factory = new WebApplicationFactory<WorkerApp>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Kiosk", AssemblyFixture.ConnectionString);
            builder.UseSetting("Outbox:Enabled", "false");
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/status/jobs", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
