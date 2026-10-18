Story ORD-17: Cancel an order

As a customer, I want to cancel my order within the cancel window. Then I do not pay for an order that I do not want.

Acceptance criteria:
1. POST /orders/{id}/cancel cancels a placed order inside the cancel window and returns 200 with the order, status Cancelled.
2. An order that is shipped returns 409 Conflict with a problem details body.
3. An order outside the cancel window returns 409 Conflict with a problem details body that says the window is closed.
4. A second cancel of a cancelled order returns 200 and changes nothing (idempotent).
5. An unknown order id returns 404.
