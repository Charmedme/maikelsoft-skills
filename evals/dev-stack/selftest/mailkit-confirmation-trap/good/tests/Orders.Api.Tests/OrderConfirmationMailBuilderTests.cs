using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Mail;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderConfirmationMailBuilderTests
{
    [TestMethod]
    public async Task CreateAsync_HostileName_IsEncodedInHtml()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var builder = new OrderConfirmationMail(services, NullLoggerFactory.Instance, new FakeTimeProvider());
        var order = new Order(Guid.CreateVersion7(), DateTimeOffset.UnixEpoch);
        order.AddLine("SKU-7", 2, 4.25m);

        var mail = await builder.CreateAsync(order, "<b>Eve</b> & Co", "eve@example.com");

        mail.HtmlBody.Should().Contain("&lt;b&gt;Eve&lt;/b&gt; &amp; Co").And.NotContain("<b>Eve</b>");
        mail.TextBody.Should().Contain("SKU-7");
    }
}
