# FL-44 Show the status of a parts order on the work order

As a planner, I want to see the status of the parts order of a work order that waits for parts, so that I can plan the next visit when the part arrives and inform the customer.

Today a planner logs in to PartsLink (the supplier system) and searches for the order. The planner often plans a visit too early, and the technician arrives without the part.

## Acceptance criteria

1. AC-1: On a work order with the status "Waiting for parts", the planner sees each ordered part with its supplier status: ordered, confirmed, shipped, delivered, or cancelled.
2. AC-2: The planner sees the expected delivery date from the supplier and the time of the last update from PartsLink.
3. AC-3: When a part is delivered, the planner can plan the next visit from the same place.
4. AC-4: When the expected date moves, or the supplier cancels a part, the planner sees this clearly.
5. AC-5: When PartsLink did not send an update for more than 24 hours, the planner sees a warning.
