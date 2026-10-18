Story ORD-41: Cancel an order until the end of the next shop day

As a customer, I want to cancel my order until the end of the day after I ordered. Then I have one full day to change my mind.

Acceptance criteria:
1. POST /orders/{id}/cancel cancels a placed order and returns 200 with the order, status Cancelled.
2. A customer can cancel until 23:59:59 shop time on the calendar day after the order day. The shop time zone is Europe/Amsterdam. The order day is the date of the order in shop time.
3. After that moment, the cancel returns 409 Conflict with a problem details body.
4. An order that is shipped returns 409 Conflict with a problem details body.
5. A second cancel of a cancelled order returns 200 and changes nothing.
6. An unknown order id returns 404.
