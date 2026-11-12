# Filter the audit log by day and user

As an auditor, I want to filter the audit log by days and by user, in my own time zone, so that I can answer "who changed what on that day".

## Acceptance criteria

1. `GET /api/auditlogentries/search` takes `from` and `to` (dates as `yyyy-MM-dd`), `timeZone` (an IANA name such as `Europe/Amsterdam`), `userName`, `action`, `page` and `pageSize`.
2. `from` and `to` are whole days in the given time zone, and both days are included. Without `timeZone`, the days are in UTC.
3. `userName` and `action` match exactly, ignoring upper and lower case. Each filter is optional.
4. The result is `{"items": [...], "totalItems": n, "page": p, "pageSize": s}`, newest entry first. Each item has `id`, `userName`, `action`, `objectId`, `createdDateTime` (UTC), and `log`.
5. `page` starts at 1. `pageSize` is 20 when not given, and at most 100.
6. Wrong input is refused with 400 and a validation error on the field: `from` after `to`, a date that is not `yyyy-MM-dd`, an unknown time zone, `page` below 1, `pageSize` below 1 or above 100.
7. The endpoint needs the permission to read audit logs, like the existing audit log endpoints.
