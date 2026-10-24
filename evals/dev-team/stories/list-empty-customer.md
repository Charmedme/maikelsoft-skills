Bug ORD-45: The order list accepts an empty customer id

State: approved

Steps to reproduce:
1. Call GET /orders?customerId=00000000-0000-0000-0000-000000000000.

Expected result: 400 Bad Request with a validation problem body that names the field customerId.

Actual result: 200 OK with an empty list. A client with a broken customer lookup does not see its error.
