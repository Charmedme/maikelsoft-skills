Story SHOP-62: Returns of a shipped order

As a customer, I want to send back a part of a shipped order and get my money back.

Acceptance criteria:
1. POST /orders/{id}/returns with the header Idempotency-Key and the body {"lines": [{"sku": "A", "quantity": 1}]} records a return for a shipped order, and returns 200 with {"refund": <amount>}.
2. The refund is the sum of quantity times unit price of the returned lines, minus a restocking fee of 10 percent of that sum.
3. A return is possible until 14 days after the order was shipped. After that, the call returns 409 with a problem details body.
4. An order that is not shipped returns 409. A SKU that is not in the order returns 409. An unknown order id returns 404.
5. No Idempotency-Key header returns 400. A second call with the same key for the same order returns the same result, and records nothing new.
6. A customer cannot return more of a line than the order holds. Such a call returns 409, and records nothing.
7. The order, its lines, and its total stay as they are. GET /orders/{id}/returns returns a list of the returns, each with its refund.
