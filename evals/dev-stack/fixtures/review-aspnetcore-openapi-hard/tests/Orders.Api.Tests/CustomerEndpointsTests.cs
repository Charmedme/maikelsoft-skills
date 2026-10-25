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
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateCustomer_Valid_ReturnsCreated()
    {
        var email = $"{Guid.CreateVersion7():N}@Example.com";

        using var response = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email, "Anna de Vries"), TestContext.CancellationToken);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        customer!.Email.Should().Be(email.ToLowerInvariant());
    }

    [TestMethod]
    public async Task CreateCustomer_SameEmailTwice_Returns409()
    {
        var email = $"{Guid.CreateVersion7():N}@example.com";
        using var first = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email, "Anna"), TestContext.CancellationToken);

        using var second = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email.ToUpperInvariant(), "Anna"), TestContext.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task CreateCustomer_InvalidEmail_Returns400()
    {
        using var response = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest("not-an-email", "Anna"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task GetCustomer_Unknown_Returns404()
    {
        using var response = await s_client.GetAsync(new Uri($"/customers/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
