using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Orders.Api.Orders;

namespace Orders.Api.Mail;

public sealed record OrderConfirmationModel(Guid OrderId, string CustomerName, decimal Total, IReadOnlyList<OrderLineResponse> Lines);

// Builds the confirmation mail. The HTML comes from a Razor component: HtmlRenderer encodes each value.
public sealed class OrderConfirmationMail(IServiceProvider services, ILoggerFactory loggerFactory, TimeProvider time)
{
    public const string Subject = "Order received";

    public async Task<OutboxMail> CreateAsync(Order order, string customerName, string email)
    {
        var model = new OrderConfirmationModel(order.Id, customerName, order.Total, order.ToResponse().Lines);
        var html = await RenderHtmlAsync(model);
        return new OutboxMail(email, customerName, Subject, RenderText(model), html, time.GetUtcNow());
    }

    private async Task<string> RenderHtmlAsync(OrderConfirmationModel model)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(OrderConfirmationTemplate.Model)] = model });
            var output = await renderer.RenderComponentAsync<OrderConfirmationTemplate>(parameters);
            return output.ToHtmlString();
        });
    }

    private static string RenderText(OrderConfirmationModel model)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"Dear {model.CustomerName},\n\nWe received your order {model.OrderId}.\n\n");
        foreach (var line in model.Lines)
        {
            text.Append(CultureInfo.InvariantCulture, $"{line.Sku}  {line.Quantity} x {line.UnitPrice:0.00} = {line.LineTotal:0.00}\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"\nTotal: {model.Total:0.00}\n");
        return text.ToString();
    }
}
