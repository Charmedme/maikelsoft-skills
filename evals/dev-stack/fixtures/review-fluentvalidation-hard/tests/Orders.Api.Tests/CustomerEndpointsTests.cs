using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static readonly AddressRequest s_address = new("Dorpsstraat 1", "1234 ab", "Borculo", "NL");

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

        using var response = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email, "Anna de Vries", s_address), TestContext.CancellationToken);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        customer!.Email.Should().Be(email.ToLowerInvariant());
        customer.Address.Should().Be(new AddressResponse("Dorpsstraat 1", "1234 AB", "Borculo", "NL"));
    }

    [TestMethod]
    public async Task CreateCustomer_SameEmailTwice_Returns400WithCode()
    {
        var email = $"{Guid.CreateVersion7():N}@example.com";
        using var first = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email, "Anna", s_address), TestContext.CancellationToken);

        using var second = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest(email.ToUpperInvariant(), "Anna", s_address), TestContext.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodesAsync(second)).Should().ContainKey("Email").WhoseValue.Should().Equal("customer.email_taken");
    }

    [TestMethod]
    public async Task CreateCustomer_InvalidEmail_Returns400()
    {
        using var response = await s_client.PostAsJsonAsync("/customers", new CreateCustomerRequest("not-an-email", "Anna", s_address), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateCustomer_CountryNotServed_Returns400WithCode()
    {
        var request = new CreateCustomerRequest($"{Guid.CreateVersion7():N}@example.com", "Anna", s_address with { CountryCode = "FR" });

        using var response = await s_client.PostAsJsonAsync("/customers", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodesAsync(response)).Should().ContainKey("Address.CountryCode").WhoseValue.Should().Equal("address.country_not_supported");
    }

    [TestMethod]
    public async Task UpdateCustomer_NewNameWithoutAddress_KeepsAddress()
    {
        var created = await CreateAsync();

        using var response = await s_client.PutAsJsonAsync($"/customers/{created.Id}", new UpdateCustomerRequest(created.Email, "Anna Jansen", null), TestContext.CancellationToken);
        var updated = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Name.Should().Be("Anna Jansen");
        updated.Address.Should().Be(created.Address);
    }

    [TestMethod]
    public async Task UpdateCustomer_NewAddress_ChangesAddress()
    {
        var created = await CreateAsync();
        var address = new AddressRequest("Marktstraat 12", "7271 AB", "Borculo", "nl");

        using var response = await s_client.PutAsJsonAsync($"/customers/{created.Id}", new UpdateCustomerRequest(created.Email, created.Name, address), TestContext.CancellationToken);
        var updated = await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Address.Should().Be(new AddressResponse("Marktstraat 12", "7271 AB", "Borculo", "NL"));
    }

    [TestMethod]
    public async Task UpdateCustomer_EmailOfOtherCustomer_Returns409()
    {
        var first = await CreateAsync();
        var second = await CreateAsync();

        using var response = await s_client.PutAsJsonAsync($"/customers/{second.Id}", new UpdateCustomerRequest(first.Email, second.Name, null), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task UpdateCustomer_Unknown_Returns404()
    {
        using var response = await s_client.PutAsJsonAsync($"/customers/{Guid.CreateVersion7()}", new UpdateCustomerRequest("anna@example.com", "Anna", null), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetCustomer_Unknown_Returns404()
    {
        using var response = await s_client.GetAsync(new Uri($"/customers/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<CustomerResponse> CreateAsync()
    {
        var request = new CreateCustomerRequest($"{Guid.CreateVersion7():N}@example.com", "Anna", s_address);
        using var response = await s_client.PostAsJsonAsync("/customers", request, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken))!;
    }

    private async Task<Dictionary<string, string[]>> ErrorCodesAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return document.RootElement.GetProperty("errorCodes").Deserialize<Dictionary<string, string[]>>()!;
    }
}
