using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new(Options.Create(new OrdersOptions { MaxLinesPerOrder = 2 }));

    [TestMethod]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m)]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void NoCustomer_HasCustomerCode()
    {
        var result = _validator.TestValidate(new CreateOrderRequest(null, [new("SKU-1", 1, 1m)]));

        result.ShouldHaveValidationErrorFor(x => x.CustomerId).WithErrorCode("order.customer_required");
    }

    [TestMethod]
    public void TooManyLines_HasLinesCode()
    {
        var result = _validator.TestValidate(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m), new("SKU-2", 1, 1m), new("SKU-3", 1, 1m)]));

        result.ShouldHaveValidationErrorFor(x => x.Lines).WithErrorCode("order.lines_too_many");
    }

    [TestMethod]
    public void QuantityZero_HasQuantityCode()
    {
        var result = _validator.TestValidate(new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 0, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity").WithErrorCode("order.quantity_out_of_range");
    }

    [TestMethod]
    public void SkuTooLong_HasSkuCode()
    {
        var result = _validator.TestValidate(new CreateOrderRequest(Guid.CreateVersion7(), [new(new string('A', 33), 1, 1m)]));

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_invalid");
    }
}
