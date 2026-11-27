using System.Globalization;
using Kiosk.Api.Data;
using Kiosk.Api.Mail;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Orders;

public sealed class OrderService(KioskDbContext db, IPaymentGateway payments, IEmailSender mail, TimeProvider time)
{
    public async Task<PlaceOrderResult> PlaceAsync(PlaceOrder request, CancellationToken ct)
    {
        if (request.Lines.Count == 0)
        {
            return new PlaceOrderResult(null, "The order has no lines.");
        }

        if (request.Lines.Any(l => l.Quantity is < 1 or > 100))
        {
            return new PlaceOrderResult(null, "Each quantity must be between 1 and 100.");
        }

        var ids = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id) && p.DeletedAt == null).ToDictionaryAsync(p => p.Id, ct);
        if (products.Count != ids.Count)
        {
            return new PlaceOrderResult(null, "The order names a product that does not exist.");
        }

        var order = new Order
        {
            OrderNumber = NewOrderNumber(),
            CustomerId = request.CustomerId,
            GuestEmail = request.GuestEmail,
            DeliveryAddress = request.DeliveryAddress,
            PartnerReference = request.PartnerReference,
            Note = request.Note,
            CreatedAt = time.GetUtcNow(),
            Lines = request.Lines.Select(l => new Orders.OrderLine
            {
                ProductId = l.ProductId,
                ProductName = products[l.ProductId].Name,
                Quantity = l.Quantity,
                UnitPrice = products[l.ProductId].Price,
            }).ToList(),
        };

        var subtotal = order.Lines.Sum(l => l.UnitPrice * l.Quantity);
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await db.Coupons.SingleOrDefaultAsync(c => c.Code == request.CouponCode && c.RedeemedAt == null, ct);
            if (coupon is null)
            {
                return new PlaceOrderResult(null, "The coupon is not valid.");
            }

            coupon.RedeemedAt = order.CreatedAt;
            order.DiscountPercent = coupon.DiscountPercent;
        }

        order.Total = Math.Round(subtotal * (100 - order.DiscountPercent) / 100m, 2);
        order.Status = await payments.AuthorizeAsync(order.OrderNumber, order.Total, ct) ? OrderStatus.Paid : OrderStatus.AwaitingPayment;
        db.Orders.Add(order);

        var recipient = request.GuestEmail ?? await db.Users.Where(u => u.Id == request.CustomerId).Select(u => u.Email).SingleOrDefaultAsync(ct);
        if (recipient is not null)
        {
            await mail.QueueAsync(recipient, $"Order {order.OrderNumber}", MailTemplates.OrderConfirmation(order.OrderNumber, order.Total), ct);
        }

        await db.SaveChangesAsync(ct);
        return new PlaceOrderResult(order, null);
    }

    private static string NewOrderNumber() =>
        "K-" + Random.Shared.Next(100_000, 1_000_000).ToString(CultureInfo.InvariantCulture);
}
