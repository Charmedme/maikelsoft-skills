# Validator tests

Unit tests for each rule and HTTP tests for the error response. The examples use MSTest 4. The TestHelper API is the same in xUnit. Each example compiled and passed with FluentValidation 12.2.0.

## Contents

- Unit tests with TestValidateAsync
- A consumer that needs a failing validator
- HTTP tests
- What not to do

## Unit tests with TestValidateAsync

Test each rule with one invalid case and its code, and one valid request with no errors. Use the real child validators and in-memory fakes of their dependencies.

```csharp
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new(
        Options.Create(new OrdersOptions { MaxLinesPerOrder = 2 }),
        new CreateOrderLineRequestValidator(new InMemoryProductCatalog()));

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Sku_Unknown_HasSkuUnknownCode()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new("NOPE", 1, 1m)]);

        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_unknown");
    }

    [TestMethod]
    public async Task Sku_Empty_HasOneError()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new("", 1, 1m)]);

        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor("Lines[0].Sku").WithErrorCode("order.sku_required").Only();
    }

    [TestMethod]
    public async Task ValidRequest_HasNoErrors()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new("SKU-1", 1, 1m)]);

        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

- Use `TestValidateAsync` for a validator with an async rule. `TestValidate` on it throws, like `Validate`.
- `.Only()` proves the cascade: an empty SKU gives one error, not two.
- A collection item has the property name `Lines[0].Sku`. Pass it as a string.
- Pass a small limit through `Options.Create(...)` to test a limit rule without 21 lines.

## A consumer that needs a failing validator

Use `InlineValidator<T>` when the code under test is the consumer (a filter, a worker), and the test needs a validator that fails:

```csharp
var failing = new InlineValidator<CreateOrderRequest>
{
    v => v.RuleFor(x => x.CustomerId).Must(_ => false).WithErrorCode("order.customer_blocked"),
};

var result = failing.TestValidate(new CreateOrderRequest(Guid.CreateVersion7(), []));

result.ShouldHaveValidationErrors();
```

## HTTP tests

One HTTP test per endpoint proves the wiring: the registration, the filter, the async call, and the codes in the response.

- Post an invalid body and assert 400, `application/problem+json`, and the code in `errorCodes`.
- Post an async-invalid body (for example an unknown SKU) and assert 400, not 500.
- Post the JSON `null` with a host in `Production` and assert 400 (`call-sites.md`, Null and Development).
- Use the real validators in the test host. Replace only their dependencies (for example the catalog), when the real dependency needs a network.

## What not to do

| Wrong | Reason | Correct |
|---|---|---|
| `Substitute.For<IValidator<T>>()` or `new Mock<IValidator<T>>()` in an endpoint test | It hides a missing registration, a sync call, and the error shape | The real validator, or `InlineValidator<T>` |
| A test that asserts the message text | Messages change and get translated | Assert the code |
| `ShouldHaveAnyValidationError()` | Removed in 12.x (CS1061) | `ShouldHaveValidationErrors()` |
| Only a happy-path test | A rule without an invalid case can be wrong or missing | One invalid case per rule |
