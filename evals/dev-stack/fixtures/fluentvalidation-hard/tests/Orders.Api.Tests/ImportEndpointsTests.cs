using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Orders.Api.Import;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ImportEndpointsTests
{
    private const string Header = "externalRef;customerId;sku;quantity;unitPrice";
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_admin = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_admin = s_factory.CreateAdminClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_admin.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Import_TwoLinesOfOneOrder_ImportsOneOrder()
    {
        var customer = Guid.CreateVersion7();

        var report = await ImportAsync($"{Header}\nA-1;{customer};SKU-1;2;18.50\nA-1;{customer};SKU-2;1;24.00");

        report.Imported.Should().Be(1);
        report.Rejected.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Import_ZeroQuantity_IsRejectedWithCode()
    {
        var report = await ImportAsync($"{Header}\nB-1;{Guid.CreateVersion7()};SKU-1;0;18.50");

        report.Imported.Should().Be(0);
        report.Rejected.Should().ContainSingle().Which.Codes.Should().Contain("order.quantity_out_of_range");
    }

    [TestMethod]
    public async Task Import_MissingCustomer_IsRejectedWithCode()
    {
        var report = await ImportAsync($"{Header}\nC-1;;SKU-1;1;18.50");

        report.Rejected.Should().ContainSingle().Which.Codes.Should().Contain("order.customer_required");
    }

    [TestMethod]
    public async Task Import_MalformedRow_IsReportedAndOthersImport()
    {
        var report = await ImportAsync($"{Header}\nD-1;{Guid.CreateVersion7()};SKU-1;x;1\nD-2;{Guid.CreateVersion7()};SKU-3;1;12.95");

        report.Imported.Should().Be(1);
        report.Rejected.Should().ContainSingle().Which.Codes.Should().Equal(CsvOrderReader.RowInvalid);
    }

    private async Task<ImportReport> ImportAsync(string csv)
    {
        using var content = new StringContent(csv, Encoding.UTF8, "text/csv");
        using var response = await s_admin.PostAsync(new Uri("/admin/orders/import", UriKind.Relative), content, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return (await response.Content.ReadFromJsonAsync<ImportReport>(TestContext.CancellationToken))!;
    }
}
