# FL-31 Assign unplanned work orders to a technician

As a planner, I want to assign several unplanned work orders to one technician and slot at the same time, so that I can plan a region faster.

Today a planner opens each work order and assigns it one by one. On a Monday a planner has 40 to 70 unplanned work orders.

## Acceptance criteria

1. AC-1: The planner sees the unplanned work orders of the region, with type, customer, address, skill needed, and due date.
2. AC-2: The planner can narrow the list by type, skill needed, and due date.
3. AC-3: The planner can select several work orders and assign them to one technician and one slot.
4. AC-4: Fieldline only offers technicians with the needed skill and a free place in the slot (max 2 visits per slot).
5. AC-5: When some work orders cannot be assigned (for example the slot is full), the planner sees which ones and why. The others are assigned.
6. AC-6: The planner can undo the assignment.
