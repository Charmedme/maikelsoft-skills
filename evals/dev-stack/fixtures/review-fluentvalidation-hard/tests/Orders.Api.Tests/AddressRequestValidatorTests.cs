using FluentValidation.TestHelper;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
public sealed class AddressRequestValidatorTests
{
    private static readonly AddressRequest s_valid = new("Dorpsstraat 1", "1234 AB", "Borculo", "NL");
    private readonly AddressRequestValidator _validator = new();

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ValidAddress_HasNoErrors()
    {
        var result = await ValidateAsync(s_valid);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task EmptyLine_HasLineRequiredCode()
    {
        var result = await ValidateAsync(s_valid with { Line = "" });

        result.ShouldHaveValidationErrorFor(x => x.Line).WithErrorCode("address.line_required").Only();
    }

    [TestMethod]
    public async Task EmptyPostalCode_HasOneError()
    {
        var result = await ValidateAsync(s_valid with { PostalCode = "" });

        result.ShouldHaveValidationErrorFor(x => x.PostalCode).WithErrorCode("address.postal_code_required").Only();
    }

    [TestMethod]
    public async Task PostalCodeWithSymbols_HasPostalCodeInvalidCode()
    {
        var result = await ValidateAsync(s_valid with { PostalCode = "12#4" });

        result.ShouldHaveValidationErrorFor(x => x.PostalCode).WithErrorCode("address.postal_code_invalid");
    }

    [TestMethod]
    public async Task CountryFrance_HasCountryNotSupportedCode()
    {
        var result = await ValidateAsync(s_valid with { CountryCode = "FR" });

        result.ShouldHaveValidationErrorFor(x => x.CountryCode).WithErrorCode("address.country_not_supported");
    }

    [TestMethod]
    public async Task LowerCaseCountry_IsValid()
    {
        var result = await ValidateAsync(s_valid with { CountryCode = "be" });

        result.ShouldNotHaveValidationErrorFor(x => x.CountryCode);
    }

    private Task<TestValidationResult<AddressRequest>> ValidateAsync(AddressRequest request) =>
        _validator.TestValidateAsync(request, cancellationToken: TestContext.CancellationToken);
}
