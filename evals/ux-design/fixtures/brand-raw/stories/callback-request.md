# FL-38 Register a repair request during a phone call

As a customer service employee, I want to register a repair request while the customer is on the phone, so that the repair gets a first visit within 2 work days.

Today customer service writes the request on paper or in an email, and a planner types it into Fieldline later. Requests get lost or have a wrong address.

## Acceptance criteria

1. AC-1: The employee finds an existing customer by name, postcode, or phone number. When the customer is new, the employee enters the name, the address, and the phone number.
2. AC-2: The employee selects the installation at the address (heat pump, air conditioning, or gas boiler) and describes the problem.
3. AC-3: The employee can mark the request as urgent (no heating, no cooling in a heat wave, or a leak).
4. AC-4: On save, Fieldline creates a repair work order with the status Unplanned and shows its ID to read to the customer.
5. AC-5: When a required field is missing or the postcode is not valid, the employee sees what to fix before the save.
6. AC-6: When the customer already has an open repair for the same installation, the employee sees it before saving a second one.
