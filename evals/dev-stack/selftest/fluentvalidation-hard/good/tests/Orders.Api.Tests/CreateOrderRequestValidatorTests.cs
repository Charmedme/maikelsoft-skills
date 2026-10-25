using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new(
        Options.Create(new OrdersOptions { MaxLinesPerOrder = 2 }),
        new CreateOrderLineRequestValidator(new FakeProductCatalog()));

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ValidRequest_HasNoErrors()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m)]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task NoCustomer_HasCustomerCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(null, [new("SKU-1", 1, 1m)]));

        result.ShouldHaveValidationErrorFor(x => x.CustomerId).WithErrorCode("order.customer_required");
    }

    [TestMethod]
    public async Task TooManyLines_HasLinesCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m), new("SKU-2", 1, 1m), new("SKU-3", 1, 1m)]));

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.lines_too_many");
    }

    [TestMethod]
    public async Task NullLines_HasLinesRequiredCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), null!));

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.lines_required").Only();
    }

    [TestMethod]
    public async Task NullLine_HasLineRequiredCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [null!]));

        result.ShouldHaveValidationErrorFor("Lines[0]").WithErrorCode("order.line_required");
    }

    [TestMethod]
    public async Task DuplicateSku_IgnoresCase()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m), new("sku-1", 1, 1m)]));

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.sku_duplicate");
    }

    [TestMethod]
    public async Task EmptySku_HasOneSkuRequiredError()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("", 1, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_required").Only();
    }

    [TestMethod]
    public async Task UnknownSku_HasSkuUnknownCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("NOPE", 1, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_unknown");
    }

    [TestMethod]
    public async Task InactiveSku_HasSkuUnknownCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-OLD", 1, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_unknown");
    }

    [TestMethod]
    public async Task QuantityZero_HasQuantityCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 0, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity").WithErrorCode("order.quantity_out_of_range");
    }

    [TestMethod]
    public async Task SkuTooLong_HasSkuCode()
    {
        var result = await ValidateAsync(new CreateOrderRequest(Guid.CreateVersion7(), [new(new string('A', 33), 1, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_invalid");
    }

    private Task<TestValidationResult<CreateOrderRequest>> ValidateAsync(CreateOrderRequest request) =>
        _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);
}
