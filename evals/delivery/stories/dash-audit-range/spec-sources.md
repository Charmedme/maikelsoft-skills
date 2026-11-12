# Sources: dash-audit-range

Repo paths are under `repo/src/Monolith/` (the app that `repo/eval/start.sh` runs) unless written in full.

## Rules

- R1 (AC1, AC2): whole days in the time zone, both included, UTC without `timeZone` — story.md AC2. `Europe/Amsterdam` — story.md AC1. Missing `from`/`to` means no limit on that side — answers.md line 3. The UTC offset of Amsterdam on 2026-03-10 (UTC+1, before the summer time switch) is a calendar fact, used as test data.
- R2 (AC2, AC6): limit 366 days counting both days, error on `to` — answers.md line 6. `from` after `to` gives an error on `to` — story.md AC6 + answers.md line 5. Status 400 — story.md AC6. Dates 2026-01-01..2027-01-02 are test data.
- R3 (AC3): exact match ignoring case, filters optional — story.md AC3. `userName` is the user who made the change — answers.md line 7. User name `admin` — repo/src/Monolith/ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs. Actions `CREATED_PRODUCT`, `UPDATED_PRODUCT` — existing audit actions, repo/src/ModularMonolith/ClassifiedAds.Modules.Product/EventHandlers/*.cs.
- R4 (AC4): result shape, item fields, `createdDateTime` in UTC, newest first — story.md AC4. `totalItems` counts all matches — answers.md line 8. Page past the end gives empty `items` with full `totalItems` — answers.md line 9. Entries with the same time can come in any order (answers.md line 10): no scenario pins a tie order; R4 uses distinct times.
- R5 (AC5, AC6): default 20, maximum 100, 400 above 100 — story.md AC5, AC6. Field name `pageSize` — answers.md line 4.
- R6 (AC5, AC6): `page` starts at 1 — AC5; errors for bad date format, unknown time zone, `page` below 1, `pageSize` below 1 — AC6. The field is the query parameter name — answers.md line 4.
- R7 (AC7): permission to read audit logs, like the existing endpoints — story.md AC7; existing endpoints use `Permission:GetAuditLogs` (ClassifiedAds.WebAPI/Endpoints/AuditLogEntriesEndpoints.cs). User `user` has only read-product permissions — TestDataSeeder.cs. 403 for a signed-in user without the permission and 401 without sign in — existing behaviour, repo/eval/smoke.sh (`user GET /api/users` 403, anonymous 401).

## Gaps

- No source gives the error message texts; scenarios check only the field.
- No source says what an empty `userName` or `action` (`userName=`) means; left out.
- No source says if `page`/`pageSize` that are not numbers give 400 on the field; left out.
- Time zone names in other case (`europe/amsterdam`) are not covered by a source; left out.
