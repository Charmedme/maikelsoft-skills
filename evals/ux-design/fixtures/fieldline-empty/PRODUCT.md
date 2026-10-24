# Fieldline

Fieldline plans the work of service technicians. The customer is a company that installs and maintains heat pumps and air conditioning for homes and small offices. It has 3 regions, 6 planners, 2 team leads, and about 45 technicians.

## Users

| User | What they do | How often | What matters |
|------|--------------|-----------|--------------|
| Planner | Assigns work orders to technicians and slots. Moves visits when a technician is ill or a part is late. Calls customers to confirm. | All day, 60 to 120 work orders a day each | Speed with many items, no double booking, see what is still open |
| Team lead | Checks the plan of the region, approves overtime, handles escalations. | A few times a day | Overview of the region, exceptions |
| Technician | Sees own visits on a phone, reports the result. | During visits | Not in this web app (separate mobile app) |
| Customer service | Takes calls, creates work orders for repairs. | All day | Fast entry while the customer is on the phone |

## Terms

| Term | Meaning |
|------|---------|
| Work order | A job for one customer address: installation, maintenance, or repair. Has an ID like WO-24817. |
| Visit | One planned moment at the address for a work order. A work order can need more than one visit. |
| Slot | A half-day block: morning (08:00 to 12:00) or afternoon (12:30 to 17:00). |
| Region | North, Central, or South. Each technician works in one region. |
| Part | A spare part. Some parts come from a supplier and take 2 to 10 work days. |
| Skill | What a technician is certified for: heat pump, air conditioning, gas boiler. |

## Facts

- A planner works on a 24-inch screen at a desk. Many planners use the keyboard a lot.
- Repairs must get a first visit within 2 work days (contract rule).
- A technician can do max 2 visits per slot.
- Parts orders go to the supplier system "PartsLink". PartsLink sends a status update a few times a day. It can take up to 4 hours before a change shows in Fieldline.
- The app is in English only for now.

## Brand

The logo is the word "Fieldline" in teal (#0b6e66). There is no design system yet.

## Constraints

- WCAG 2.2 AA.
