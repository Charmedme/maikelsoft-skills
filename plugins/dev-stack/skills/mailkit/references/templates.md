# Mail bodies and templates

How to build the text body, the HTML body, and attachments (decision MAIL-03). The code was compiled with .NET SDK 10.0.402 and MimeKit 4.18.1, and the HTML output was checked in a Mailpit test.

## Contents

- Text and HTML
- Razor component with HtmlRenderer
- Without Razor
- Attachments and images
- Subject and headers

## Text and HTML

Send both a text body and an HTML body (`multipart/alternative`). Some clients and spam filters read only the text part. `BodyBuilder` makes the correct MIME structure:

```csharp
message.Body = new BodyBuilder { TextBody = text, HtmlBody = html }.ToMessageBody();
```

Build the text body with a `StringBuilder` and `CultureInfo.InvariantCulture`, or with a fixed culture of the customer. Text needs no HTML encoding.

## Razor component with HtmlRenderer

A Razor component (`.razor`) compiles in each project that uses `Microsoft.NET.Sdk.Web`. No package is needed. Razor encodes each `@value`, so a customer name `<b>Eve</b>` becomes `&lt;b&gt;Eve&lt;/b&gt;`.

`Mail/OrderConfirmationTemplate.razor`:

```razor
@* Razor encodes each value. Do not use MarkupString for user data. *@
<html>
<body>
    <p>Dear @Model.CustomerName,</p>
    <p>We received your order @Model.OrderId.</p>
    <table>
        <tr><th>SKU</th><th>Quantity</th><th>Price</th></tr>
        @foreach (var line in Model.Lines)
        {
            <tr><td>@line.Sku</td><td>@line.Quantity</td><td>@line.LineTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)</td></tr>
        }
    </table>
    <p>Total: @Model.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)</p>
</body>
</html>

@code {
    [Parameter, EditorRequired]
    public OrderConfirmationModel Model { get; set; } = null!;
}
```

Render it with `HtmlRenderer` on its dispatcher:

```csharp
public sealed record OrderConfirmationModel(Guid OrderId, string CustomerName, decimal Total, IReadOnlyList<OrderLineResponse> Lines);

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
```

Usings: `Microsoft.AspNetCore.Components`, `Microsoft.AspNetCore.Components.Web`, `System.Globalization`, `System.Text`. Register the class as scoped: `builder.Services.AddScoped<OrderConfirmationMail>();`.

Rules for templates:

- Use `@value` for each user value. `MarkupString` and `@((MarkupString)x)` skip the encoding: use them only for HTML that the code made.
- Keep CSS inline or in a `<style>` block. Many mail clients remove external style sheets and scripts.
- Put links with user data through `Uri.EscapeDataString` for the query part, and use absolute `https` URLs.
- Test the template with a hostile name (`<b>Eve</b> & Co`) and check the encoded output.

## Without Razor

For one short mail without a template file, encode each value with `HtmlEncoder.Default.Encode(value)` (`System.Text.Encodings.Web`):

```csharp
var html = HtmlEncoder.Default;
var body = $"<p>Dear {html.Encode(customerName)}, shipment {html.Encode(reference)} has a new status.</p>";
```

Scriban and Fluid do not encode by default. When the team chooses one of them (MAIL-03), turn on the HTML encoder of the engine and test it with a hostile value. `HtmlToHtml.FilterHtml` of MimeKit is not an XSS filter.

## Attachments and images

```csharp
var builder = new BodyBuilder { TextBody = text, HtmlBody = html };
builder.Attachments.Add("invoice-2026-0042.pdf", pdfBytes, new ContentType("application", "pdf"));
message.Body = builder.ToMessageBody();
```

- Give an attachment a file name that the code makes, not a name from user input.
- Keep large files out of the outbox row. Store a blob reference, and read the bytes in the sender.
- For an inline image, use `builder.LinkedResources.Add(...)` and set its `ContentId`, then `<img src="cid:...">`. Remote images are often blocked by mail clients.

## Subject and headers

- Set `message.Subject` with the typed setter. MimeKit removes CR and LF from the subject.
- Do not call `message.Headers.Add` with user input.
- Set `message.MessageId` to a stable value for each outbox row (`{outboxId:N}@{your domain}`), so that a retry keeps the same ID.
