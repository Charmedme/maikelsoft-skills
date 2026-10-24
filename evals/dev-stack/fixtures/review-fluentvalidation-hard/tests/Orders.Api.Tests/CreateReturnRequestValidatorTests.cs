using FluentValidation.TestHelper;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CreateReturnRequestValidatorTests
{
    private readonly CreateReturnRequestValidator _validator = new();

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ValidRequest_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(
            new CreateReturnRequest(Guid.CreateVersion7(), "SKU-1", 1, ReturnReason.Damaged, null), cancellationToken: TestContext.CancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task MissingReason_HasReasonCode()
    {
        var result = await _validator.TestValidateAsync(
            new CreateReturnRequest(Guid.CreateVersion7(), "SKU-1", 1, null, null), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Reason).WithErrorCode("return.reason_required");
    }

    [TestMethod]
    public async Task UnknownReason_HasReasonUnknownCode()
    {
        var result = await _validator.TestValidateAsync(
            new CreateReturnRequest(Guid.CreateVersion7(), "SKU-1", 1, (ReturnReason)42, null), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Reason).WithErrorCode("return.reason_unknown");
    }

    [TestMethod]
    public async Task CommentTooLong_HasCommentCode()
    {
        var result = await _validator.TestValidateAsync(
            new CreateReturnRequest(Guid.CreateVersion7(), "SKU-1", 1, ReturnReason.Damaged, new string('x', 501)), cancellationToken: TestContext.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Comment).WithErrorCode("return.comment_too_long");
    }
}
