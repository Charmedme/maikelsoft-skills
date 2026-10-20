using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AddressValidatorTests
{
    private static OrdersApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task PostalCode_WrongFormat_HasError()
    {
        await using var db = NewContext();
        var validator = new AddressValidator(new InMemoryPostalCodeLookup(), db);

        var result = await validator.TestValidateAsync(Request(postalCode: "ABCD12"), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.PostalCode).WithErrorCode("address.postal_code_invalid");
    }

    [TestMethod]
    public async Task Country_NotNetherlands_HasError()
    {
        await using var db = NewContext();
        var validator = new AddressValidator(new InMemoryPostalCodeLookup(), db);

        var result = await validator.TestValidateAsync(Request(country: "DE"), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [TestMethod]
    public async Task ChangeAddress_ValidRequest_ReturnsNoContent()
    {
        var validator = Substitute.For<IValidator<ChangeAddressRequest>>();
        validator.Validate(Arg.Any<ChangeAddressRequest>()).Returns(new ValidationResult());
        await using var factory = s_factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton(validator)));
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 1m)]), TestContext.CancellationToken);
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);

        var response = await client.PutAsJsonAsync($"/orders/{order!.Id}/address", Request(orderId: order.Id), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static ChangeAddressRequest Request(Guid? orderId = null, string postalCode = "7271 AA", string country = "NL") =>
        new(orderId ?? Guid.CreateVersion7(), "Hoofdstraat 1", postalCode, "Borculo", country);

    private static OrdersDbContext NewContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options);
}
