using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Api.Customers;
using Orders.Api.Infrastructure;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ValidationFilterTests
{
    [TestMethod]
    public async Task InvalidArgument_ReturnsCodesAndSkipsHandler()
    {
        var validator = new InlineValidator<UpdateCustomerRequest>();
        validator.RuleFor(x => x.Name).Must(_ => false).WithErrorCode("test.rejected");
        var filter = new ValidationFilter<UpdateCustomerRequest>(validator);
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new UpdateCustomerRequest("anna@example.com", "Anna", null));
        var handlerRan = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            handlerRan = true;
            return ValueTask.FromResult<object?>(TypedResults.Ok());
        });

        handlerRan.Should().BeFalse();
        var problem = result.Should().BeOfType<ValidationProblem>().Subject;
        problem.ProblemDetails.Extensions["errorCodes"].Should().BeEquivalentTo(new Dictionary<string, string[]> { ["Name"] = ["test.rejected"] });
    }

    [TestMethod]
    public async Task ValidArgument_RunsHandler()
    {
        var filter = new ValidationFilter<UpdateCustomerRequest>(new InlineValidator<UpdateCustomerRequest>());
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new UpdateCustomerRequest("anna@example.com", "Anna", null));

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("handler"));

        result.Should().Be("handler");
    }
}
