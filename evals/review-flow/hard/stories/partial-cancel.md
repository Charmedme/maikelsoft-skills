Story SHOP-60: Cancel part of an order

As a customer, I want to cancel a part of a line of my order within the cancel window. Then I keep the rest of the order.

Acceptance criteria:
1. POST /orders/{id}/lines/{sku}/cancel with the body {"quantity": N} lowers the quantity of that line by N. It returns 200 with the order. The total in the response is the new total.
2. When N is the full quantity of the line, the line is no longer in the response.
3. When the order has no lines left, its status is Cancelled.
4. A shipped order returns 409 with a problem details body. An order outside the cancel window returns 409 with a problem details body.
5. An unknown order id or an unknown SKU in the order returns 404.
6. N larger than the quantity of the line returns 409, and nothing changes. N below 1 returns 400.
