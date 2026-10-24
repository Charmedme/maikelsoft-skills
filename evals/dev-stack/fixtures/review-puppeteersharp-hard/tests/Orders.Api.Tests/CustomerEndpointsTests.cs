using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CustomerEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateCustomer_Business_ReturnsCompanyName()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);

        customer.Kind.Should().Be("Business");
        customer.CompanyName.Should().Be("Example Trading B.V.");
    }

    [TestMethod]
    public async Task CreateCustomer_BusinessWithoutCompany_ReturnsValidationProblem()
    {
        var request = new CreateCustomerRequest("Bob", "bob@example.com", CustomerKind.Business, null);

        var response = await s_client.PostAsJsonAsync("/customers", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateCustomer_InvalidEmail_ReturnsValidationProblem()
    {
        var request = new CreateCustomerRequest("Bob", "not-an-address", CustomerKind.Consumer, null);

        var response = await s_client.PostAsJsonAsync("/customers", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task GetCustomer_Unknown_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri($"/customers/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task ChangeEmail_ValidAddress_StoresNewAddress()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);

        var response = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/email", new ChangeEmailRequest("new@example.com"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken))!.Email.Should().Be("new@example.com");
    }
}
