using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orders.Api.Catalog;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator;

    public CreateOrderRequestValidatorTests()
    {
        var catalog = Substitute.For<IProductCatalog>();
        catalog.IsActiveAsync(Arg.Is<string>(sku => sku.StartsWith("SKU-", StringComparison.Ordinal)), Arg.Any<CancellationToken>()).Returns(true);
        _validator = new(Options.Create(new OrdersOptions { MaxLinesPerOrder = 2 }), new CreateOrderLineRequestValidator(catalog));
    }

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ValidRequest_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task NoCustomer_HasCustomerCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(null, [new("SKU-1", 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.CustomerId).WithErrorCode("order.customer_required");
    }

    [TestMethod]
    public async Task TooManyLines_HasLinesCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m), new("SKU-2", 1, 1m), new("SKU-3", 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.lines_too_many");
    }

    [TestMethod]
    public async Task QuantityZero_HasQuantityCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 0, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity").WithErrorCode("order.quantity_out_of_range");
    }

    [TestMethod]
    public async Task SkuTooLong_HasSkuCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new(new string('A', 33), 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_invalid");
    }

    [TestMethod]
    public async Task UnknownSku_HasSkuUnknownCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("NOPE", 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_unknown");
    }

    [TestMethod]
    public async Task DuplicateSku_HasDuplicateCode()
    {
        var result = await _validator.TestValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m), new("SKU-1", 1, 1m)]), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.sku_duplicate");
    }
}
