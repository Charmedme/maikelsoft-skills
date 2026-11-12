Story SHOP-59: Order summary

As a support agent, I want a short summary of an order.

Acceptance criteria:
1. GET /orders/{id}/summary returns 200 with {"lineCount": <number of lines>, "unitCount": <sum of the quantities>, "total": <order total>}.
2. An unknown order id returns 404.
