using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Payments;

public sealed class PaymentService(OrdersDbContext db, IPaymentGateway gateway, IOptions<PaymentsOptions> options, TimeProvider time, ILogger<PaymentService> logger)
{
    public async Task<PayOutcome> PayAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return PayOutcome.NotFound();
        }

        if (order.Status != OrderStatus.Placed)
        {
            return PayOutcome.NotPayable();
        }

        if (await db.Payments.AnyAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Authorized, ct))
        {
            return PayOutcome.AlreadyPaid();
        }

        var currency = options.Value.Currency;
        var result = await gateway.AuthorizeAsync(new AuthorizationRequest(order.Id, order.Total, currency), ct);
        var payment = result.Authorized
            ? Payment.Authorized(order.Id, order.Total, currency, result.Reference!, time.GetUtcNow())
            : Payment.Declined(order.Id, order.Total, currency, result.DeclineReason!, time.GetUtcNow());
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        if (!result.Authorized)
        {
            logger.LogWarning("Payment for order {OrderId} was declined: {DeclineReason}", order.Id, result.DeclineReason);
        }

        return PayOutcome.Done(payment);
    }
}

public sealed record PayOutcome(PayOutcomeKind Kind, Payment? Payment)
{
    public static PayOutcome NotFound() => new(PayOutcomeKind.NotFound, null);

    public static PayOutcome NotPayable() => new(PayOutcomeKind.NotPayable, null);

    public static PayOutcome AlreadyPaid() => new(PayOutcomeKind.AlreadyPaid, null);

    public static PayOutcome Done(Payment payment) => new(PayOutcomeKind.Done, payment);
}

public enum PayOutcomeKind
{
    Done,
    NotFound,
    NotPayable,
    AlreadyPaid,
}
