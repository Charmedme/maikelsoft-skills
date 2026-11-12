# Domain rules

These rules hold for each change of this service. The product owner keeps them.

## Order events

Each change to an order, or to a line of an order, writes one or more rows in `order_events`, in the same `SaveChanges` as the change. Other systems read this table.

## Stock

An order reserves stock when it is placed. Everything that gives an ordered quantity back releases that quantity. A SKU with no stock row has no limit.

The sum of the available stock and the reserved quantities of a SKU must stay the same when requests run at the same time.

## Money

Money has 2 decimals. A half cent rounds away from zero. Round the result for the whole order or the whole operation one time, and never round for each line.

## Time windows

Each time window includes its exact end. A date without a time means the whole day in UTC, until the last moment of that day.

## Codes

A code that a person types compares without case, and with no spaces around it.

## What a cancel gives back

A cancel gives back everything that the order holds: stock and each limited right that the order used.

## Conflicts

Two requests can change the same data at the same time. The loser gets 409. No request may lose or double a change.
