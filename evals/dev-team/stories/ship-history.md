Story ORD-40: Ship an order and show its history

As a warehouse clerk, I want to ship an order with a carrier and a tracking code. As a customer, I want to see what happened to my order.

Acceptance criteria:
1. POST /orders/{id}/ship with the body { "carrier": "DHL", "trackingCode": "JD014600006751" } ships a placed order. It returns 200 with the order, status Shipped, and the fields carrier and trackingCode.
2. The carrier is one of PostNL, DHL, or UPS. The tracking code has 8 to 30 letters or digits. Other values return 400 with validation problem details.
3. An order that is not placed (for example an order that is already shipped) cannot ship. The ship request returns 409 with problem details.
4. GET /orders/{id}/history returns the events of the order, oldest first. Each event has the fields type (Placed or Shipped) and occurredAt (UTC).
5. GET /orders?customerId={id}&status=Shipped returns only the orders of that customer with that status. Without status, the list stays as it is today.
6. An unknown order id returns 404 on ship and on history.
