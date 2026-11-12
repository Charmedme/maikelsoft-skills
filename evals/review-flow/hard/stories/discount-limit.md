Story SHOP-61: Discount codes with a limit

As a marketing manager, I want discount codes with an end date and a maximum number of uses.

Acceptance criteria:
1. PUT /discount-codes creates or replaces a code, and returns 200 OK. An example of the body: {"code": "SPRING", "percent": 10, "expiresOn": "2026-12-31", "maxUses": 100}.
2. POST /orders accepts an optional field "discountCode". The order then has a discount of that percentage of the sum of its lines.
3. The order response has the new field "discount", which is 0 with no code. "total" is the sum of the lines minus the discount. GET /orders/{id} and GET /orders?customerId= show the same two values.
4. An unknown code returns 409 with a problem details body, and no order exists. An expired code or a code with no uses left gives the same result.
5. A code gets at most maxUses orders.
