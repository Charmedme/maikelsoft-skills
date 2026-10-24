Story ORD-50: Discount code on a new order

State: approved

As a customer, I want to enter a discount code when I place an order, so that I pay the lower price of a promotion.

For this release the shop has two codes:
- WELCOME10: 10 percent, valid until 2026-12-31 (to the end of that day, UTC).
- SPRING25: 25 percent, valid until 2026-05-31 (to the end of that day, UTC).

Acceptance criteria:
- [ ] AC1: POST /orders accepts an optional field discountCode. A valid code lowers the total of the order by the percentage of the code.
- [ ] AC2: The discount is a percentage of the subtotal (the sum of the line totals). The discount is rounded to 2 decimals, half away from zero.
- [ ] AC3: A code is not case-sensitive, and spaces at the start and the end do not count.
- [ ] AC4: An unknown code or a code after its end date returns 422 with a problem details body that names the field discountCode. The order is not stored.
- [ ] AC5: A code works only when the subtotal is 50.00 or more. A lower subtotal returns 422 with a problem details body that names the field discountCode.
- [ ] AC6: The order in the response has the fields subtotal, discount, and total. Without a code, discount is 0.00 and total equals subtotal.
- [ ] AC7: The order list (GET /orders?customerId=...) shows the same subtotal, discount, and total as the response of POST /orders.

Examples:

AC1, AC2: discount on the subtotal
- Given a new order with the lines 2 x 24.99 and 1 x 0.07 (subtotal 50.05)
- When the customer places it with the code WELCOME10
- Then the response has subtotal 50.05, discount 5.01, and total 45.04

AC3: the code is not case-sensitive
- Given a new order with the line 1 x 80.00
- When the customer places it with the code " welcome10 "
- Then the response has subtotal 80.00, discount 8.00, and total 72.00

AC4: expired and unknown codes
| Code | Result |
|------|--------|
| SPRING25 | 422, the field discountCode, no order stored |
| NOPE | 422, the field discountCode, no order stored |

AC5: the minimum subtotal
| Lines | Code | Result |
|-------|------|--------|
| 1 x 50.00 | WELCOME10 | 201, discount 5.00, total 45.00 |
| 1 x 49.99 | WELCOME10 | 422, the field discountCode |

AC6: no code
- Given a new order with the line 3 x 10.00
- When the customer places it without a code
- Then the response has subtotal 30.00, discount 0.00, and total 30.00

AC7: the list shows the discount
- Given the customer placed the order of the AC1 example
- When the customer gets the order list
- Then the order in the list has subtotal 50.05, discount 5.01, and total 45.04
