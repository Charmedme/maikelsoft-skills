# Sources: dash-lost-update

Repo paths are under `repo/src/Monolith/` unless written in full.

## Rules

- R1 (AC1): `rowVersion` on `GET /api/products/{id}` and on the list — AC1. Products PD002, PD003, PD004 — ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs.
- R2 (AC2, AC4): update needs the starting `rowVersion` — AC2. Without it 400 with a validation error on `RowVersion` — AC4. The client sends back exactly what it got (opaque string) — answers.md line 3; "v1" is test data for that opaque value. Delete needs no version — answers.md line 5. Delete reply 200 — existing behaviour, ClassifiedAds.WebAPI/Endpoints/ProductsEndpoints.cs (`Results.Ok()`) and repo/eval/smoke.sh. Name "Galaxy S24" — TestDataSeeder.cs.
- R3 (AC3): changed since that version gives 409 and nothing changes — AC3. 409 body is problem details, text free — answers.md line 4. Audit log only for the update that succeeded — answers.md line 6.
- R4 (AC5): new `rowVersion` in the response or a new GET — AC5.
- R5 (AC6): two updates from the same version at the same moment, one 200, one 409 — AC6. Success code 200 for PUT — existing behaviour, ProductsEndpoints.cs and repo/eval/smoke.sh. One audit entry only — answers.md line 6. Action `UPDATED_PRODUCT` — repo/src/ModularMonolith/ClassifiedAds.Modules.Product/EventHandlers/ProductUpdatedEventHandler.cs.

## Gaps

- No source says what happens with a `rowVersion` that is not valid (not a version the server made, for example "abc"): 400 or 409; left out.
- No source says what an empty string `rowVersion` gives (400 like missing, or 409); left out.
- No source gives the order of checks when the product does not exist and the version is old (404 or 409); left out.
