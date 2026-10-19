using Microsoft.AspNetCore.Http.HttpResults;
using MimeKit;
using Orders.Api.Data;
using Orders.Api.Mail;

namespace Orders.Api.Orders;

public sealed record ShipOrderRequest(string Email);

public static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShipping(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id:guid}/ship", ShipAsync).WithTags("Orders");
        return app;
    }

    private static async Task<Results<NoContent, NotFound>> ShipAsync(
        Guid id,
        ShipOrderRequest request,
        OrdersDbContext db,
        MailSender sender,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        order.MarkShipped();
        await db.SaveChangesAsync(ct);

        // Do not let a slow relay delay the response.
        var message = sender.NewMessage();
        message.To.Add(MailboxAddress.Parse(request.Email));
        message.Subject = $"Order {order.Id} is shipped";
        message.Body = new TextPart("plain") { Text = "Your order is on its way." };
        _ = Task.Run(() => sender.SendAsync(message, CancellationToken.None), CancellationToken.None);

        return TypedResults.NoContent();
    }
}
