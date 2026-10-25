using Orders.Api.Contracts;
using Orders.Api.Customers;
using Orders.Api.Orders;

namespace Orders.Api.Shipping;

public static class ParcelBooking
{
    public static CarrierBookingRequest Request(Shipment shipment, Customer customer, DeliveryAddress address, ShippingSpeed speed) =>
        new(shipment.Id.ToString("N"), customer.DisplayName, address, speed);

    // The messages that follow a booking: the event for other services and the pick order for the warehouse.
    public static (ShipmentBooked Booked, PickOrder Pick) Messages(Shipment shipment, Order order) =>
        (new ShipmentBooked(shipment.Id, order.Id, shipment.Carrier!, shipment.TrackingNumber!),
         new PickOrder(order.Id, shipment.Id, shipment.Carrier!, shipment.TrackingNumber!, [.. order.Lines.Select(l => new PickLine(l.Sku, l.Quantity))]));
}
