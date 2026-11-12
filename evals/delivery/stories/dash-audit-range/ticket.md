# Filter the audit log by day and user

**Type:** story
**Status:** spec:ready

As an auditor, I want to filter the audit log by days and by user, in my own time zone, so that I can answer "who changed what on that day".

## Acceptance criteria

1. `GET /api/auditlogentries/search` takes `from` and `to` (dates as `yyyy-MM-dd`), `timeZone` (an IANA name such as `Europe/Amsterdam`), `userName`, `action`, `page` and `pageSize`.
2. `from` and `to` are whole days in the given time zone, and both days are included. Without `timeZone`, the days are in UTC.
3. `userName` and `action` match exactly, ignoring upper and lower case. Each filter is optional.
4. The result is `{"items": [...], "totalItems": n, "page": p, "pageSize": s}`, newest entry first. Each item has `id`, `userName`, `action`, `objectId`, `createdDateTime` (UTC), and `log`.
5. `page` starts at 1. `pageSize` is 20 when not given, and at most 100.
6. Wrong input is refused with 400 and a validation error on the field: `from` after `to`, a date that is not `yyyy-MM-dd`, an unknown time zone, `page` below 1, `pageSize` below 1 or above 100.
7. The endpoint needs the permission to read audit logs, like the existing audit log endpoints.

## Behaviour spec

```gherkin
Rule: R1 From and to are whole days in the time zone of the auditor, both days included (AC1, AC2)

  Scenario: entry just after midnight in Amsterdam is on that day
    Given an audit log entry made at 2026-03-09 23:30 UTC, which is 2026-03-10 00:30 in Europe/Amsterdam
    When the auditor searches with from 2026-03-10, to 2026-03-10 and timeZone Europe/Amsterdam
    Then the entry is in the items

  Scenario: entry just after midnight in Amsterdam is not on the day before
    Given an audit log entry made at 2026-03-10 23:30 UTC, which is 2026-03-11 00:30 in Europe/Amsterdam
    When the auditor searches with from 2026-03-10, to 2026-03-10 and timeZone Europe/Amsterdam
    Then the entry is not in the items

  Scenario: without a time zone the days are in UTC
    Given an audit log entry made at 2026-03-10 23:30 UTC
    When the auditor searches with from 2026-03-10 and to 2026-03-10 and no timeZone
    Then the entry is in the items

  Scenario: a missing from gives no limit on that side
    Given an audit log entry made at 2025-01-02 10:00 UTC
    When the auditor searches with only to 2026-03-10
    Then the entry is in the items

Rule: R2 The range is at most 366 days, counting both days, and from is not after to (AC2, AC6) (limit: 366 days)

  Scenario: range below the limit
    When the auditor searches with from 2026-01-01 and to 2026-12-31, which is 365 days
    Then the reply is 200

  Scenario: range at the limit
    When the auditor searches with from 2026-01-01 and to 2027-01-01, which is 366 days
    Then the reply is 200

  Scenario: range above the limit
    When the auditor searches with from 2026-01-01 and to 2027-01-02, which is 367 days
    Then the reply is 400 with a validation error on to

  Scenario: from after to
    When the auditor searches with from 2026-03-11 and to 2026-03-10
    Then the reply is 400 with a validation error on to

Rule: R3 User name and action match exactly, ignoring upper and lower case, and each filter is optional (AC3)

  Scenario: user name in other case matches
    Given an audit log entry of a change that the user admin made
    When the auditor searches with userName ADMIN
    Then the entry is in the items

  Scenario: part of a user name does not match
    Given an audit log entry of a change that the user admin made
    When the auditor searches with userName adm
    Then the items list is empty

  Scenario: action in other case matches
    Given an audit log entry with action UPDATED_PRODUCT and one with action CREATED_PRODUCT
    When the auditor searches with action updated_product
    Then only the entry with action UPDATED_PRODUCT is in the items

  Scenario: no filters give all entries
    Given 3 audit log entries
    When the auditor searches without any filter
    Then totalItems is 3

Rule: R4 The result is one page of the matching entries, newest first, with the count of all matches (AC4)

  Scenario: newest entry first
    Given audit log entries made at 2026-03-10 09:00 UTC and at 2026-03-10 11:00 UTC
    When the auditor searches without any filter
    Then the first item is the entry of 2026-03-10 11:00 UTC

  Scenario: each item has its fields with the time in UTC
    Given an audit log entry made at 2026-03-09 23:30 UTC
    When the auditor searches with timeZone Europe/Amsterdam
    Then the item has id, userName, action, objectId, log and createdDateTime 2026-03-09T23:30:00Z

  Scenario: totalItems counts all matches, not only the page
    Given 25 matching audit log entries
    When the auditor asks for page 3 with pageSize 10
    Then the reply has 5 items, totalItems 25, page 3 and pageSize 10

  Scenario: page past the end
    Given 25 matching audit log entries
    When the auditor asks for page 4 with pageSize 10
    Then the reply has an empty items list and totalItems 25

Rule: R5 The page size is 20 when not given and at most 100 (AC5, AC6) (limit: 100)

  Scenario: page size not given
    Given 30 matching audit log entries
    When the auditor searches without pageSize
    Then the reply has 20 items and pageSize 20

  Scenario: page size below the limit
    Given 150 matching audit log entries
    When the auditor searches with pageSize 99
    Then the reply has 99 items

  Scenario: page size at the limit
    Given 150 matching audit log entries
    When the auditor searches with pageSize 100
    Then the reply has 100 items

  Scenario: page size above the limit
    When the auditor searches with pageSize 101
    Then the reply is 400 with a validation error on pageSize

Rule: R6 Wrong input is refused with 400 and a validation error on the query parameter (AC5, AC6)

  Scenario Outline: wrong value of one parameter
    When the auditor searches with <parameter> <value>
    Then the reply is 400 with a validation error on <parameter>

    Examples:
      | parameter | value           |
      | from      | 10-03-2026      |
      | timeZone  | Europe/Amstrdam |
      | page      | 0               |
      | pageSize  | 0               |

Rule: R7 Only a user with the permission to read audit logs can search (AC7)

  Scenario: user without the permission
    Given the user "user", who can only read products
    When the user searches the audit log
    Then the reply is 403

  Scenario: request without sign in
    When an anonymous visitor searches the audit log
    Then the reply is 401
```
