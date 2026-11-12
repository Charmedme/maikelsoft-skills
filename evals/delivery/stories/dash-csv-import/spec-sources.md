# Sources: dash-csv-import

Repo paths are under `repo/src/Monolith/` unless written in full.

## Rules

- R1 (AC1): header `Code,Name,Description`, standard CSV quoting with doubled quotes, UTF-8 BOM allowed — story.md AC1. Another header refused with 400 as a whole — answers.md line 3. Empty lines skipped and not counted — answers.md line 4.
- R2 (AC2): match by code trimmed and ignoring case updates, else create — story.md AC2. Code stored trimmed, name and description as written — answers.md line 6. Description can be empty — answers.md line 5. Product PD002 "Galaxy S24" / "Samsung phone" — seed data, ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs.
- R3 (AC3, AC4): all or nothing, 400 with the errors by line (header is line 1) — AC3. Empty code or name, duplicate code reports both lines — AC4. Error body is problem details with an `errors` list, each item `line` and `message` — answers.md line 8.
- R4 (AC4): name at most 100 characters (limit: 100 characters) — AC4.
- R5 (AC5): more than 1000 product lines refused with 400 as a whole (limit: 1000 lines) — AC5. Empty lines do not count — answers.md line 4.
- R6 (AC6, AC7): reply 200 with `{"created", "updated", "unchanged"}`, unchanged changes nothing — AC6. Same file twice gives all unchanged — answers.md line 7. Same audit log entries as create/update on the screen, none for unchanged — AC7. Action names `CREATED_PRODUCT`, `UPDATED_PRODUCT` — existing behaviour, repo/src/ModularMonolith/ClassifiedAds.Modules.Product/EventHandlers/ProductCreatedEventHandler.cs and ProductUpdatedEventHandler.cs. Products PD003 "Pixel 9"/"Google phone" — TestDataSeeder.cs.
- R7 (AC8): both permissions needed, else 403 — AC8. Users `admin` (all permissions) and `user` (read products only) — TestDataSeeder.cs.

## Gaps

- No source gives the `message` texts of the errors; scenarios check only `line`.
- No source says if an empty line counts in the line numbers of the errors; left out.
- No source says if a code of only spaces counts as empty (AC4 says "empty", AC2 trims for the match); left out.
- No source gives the reply for a missing file or a file that is not valid CSV (an open quote); left out.
- No source gives the error body for the "as a whole" refusals (wrong header, more than 1000 lines); scenarios check only 400.
