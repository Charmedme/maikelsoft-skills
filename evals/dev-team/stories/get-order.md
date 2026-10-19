Story ORD-12: Read one order

As a customer, I want to see one of my orders, so that I can check its lines and its total.

Acceptance criteria:
1. GET /orders/{id} returns 200 with the order, its lines, and its total.
2. An unknown order id returns 404.
