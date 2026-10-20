using Orders.Api.Contracts;
using Orders.Api.Orders;
using Wolverine;

namespace Orders.Api.Shipping;

public sealed class ShippingService(IMessageBus bus, TimeProvider time)
{
    public async Task ShipAsync(Order order)
    {
        order.MarkShipped();
        await bus.PublishAsync(new OrderShipped(order.Id, time.GetUtcNow()));
    }
}
