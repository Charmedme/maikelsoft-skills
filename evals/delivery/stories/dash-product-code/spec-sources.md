# Sources: dash-product-code

Repo paths are under `repo/src/Monolith/` unless written in full.

## Rules

- R1 (AC1): stored without white space at start and end — AC1. Case kept as typed, only trimmed — answers.md line 3. "Ab-100" style codes — story.md Context (`ab-100`, `AB-100`).
- R2 (AC1, AC3): empty code or only spaces refused with 400 and an error on `Code` — answers.md line 4. The answer names no endpoint, so the scenarios cover create and update.
- R3 (AC2, AC3): same after trim ignoring case — AC2; create refuses with 400 and a validation error on `Code` — AC3. Products Galaxy S24/PD002, Pixel 9/PD003, ThinkPad X1/PD004 — ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs. Create reply 201 — existing behaviour, ClassifiedAds.WebAPI/Endpoints/ProductsEndpoints.cs and repo/eval/smoke.sh.
- R4 (AC3, AC4): update refuses another product's code — AC3; a product keeps its own code, also with only a case change — AC4. Update reply 200 — existing behaviour (same files). The stored case after a case change is "as typed" — answers.md line 3.
- R5 (AC5): code of a deleted product can be used again — AC5.
- R6 (AC6): race, only one product gets the code — AC6. The loser gets the same 400 as a normal duplicate — answers.md line 6.
- Existing duplicates were cleaned up (answers.md line 5): no migration scenario needed.

## Gaps

- No source gives a maximum length for the code; left out.
- No source gives the text of the validation message; scenarios check only the field.
- No source says if the import from CSV must follow the same rule; left out (other story).
