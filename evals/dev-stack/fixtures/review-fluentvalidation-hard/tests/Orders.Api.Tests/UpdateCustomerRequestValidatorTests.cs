using FluentValidation.TestHelper;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
public sealed class UpdateCustomerRequestValidatorTests
{
    private readonly UpdateCustomerRequestValidator _validator = new(new AddressRequestValidator());

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ValidRequestWithoutAddress_HasNoErrors()
    {
        var result = await ValidateAsync(new UpdateCustomerRequest("anna@example.com", "Anna", null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task EmptyEmail_HasEmailRequiredCode()
    {
        var result = await ValidateAsync(new UpdateCustomerRequest("", "Anna", null));

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode("customer.email_required").Only();
    }

    [TestMethod]
    public async Task InvalidEmail_HasEmailInvalidCode()
    {
        var result = await ValidateAsync(new UpdateCustomerRequest("anna", "Anna", null));

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode("customer.email_invalid");
    }

    [TestMethod]
    public async Task LongName_HasNameTooLongCode()
    {
        var result = await ValidateAsync(new UpdateCustomerRequest("anna@example.com", new string('a', 101), null));

        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorCode("customer.name_too_long");
    }

    [TestMethod]
    public async Task AddressWithEmptyCity_HasCityCode()
    {
        var result = await ValidateAsync(new UpdateCustomerRequest("anna@example.com", "Anna", new AddressRequest("Dorpsstraat 1", "1234 AB", "", "NL")));

        result.ShouldHaveValidationErrorFor("Address.City").WithErrorCode("address.city_required");
    }

    private Task<TestValidationResult<UpdateCustomerRequest>> ValidateAsync(UpdateCustomerRequest request) =>
        _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);
}
