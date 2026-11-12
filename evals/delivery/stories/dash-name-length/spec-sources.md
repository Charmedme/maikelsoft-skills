# Sources: dash-name-length

Repo paths are under `repo/src/Monolith/` unless written in full.

## Rules

- R1 (AC1, AC2, AC3): at most 100 characters (limit: 100 characters), create refuses longer with 400 and an error on `Name`, exactly 100 accepted — AC1, AC2, AC3. Characters counted as the user sees them, plain letters enough — answers.md line 3, so the test names use plain letters. Error format is the existing validation problem — answers.md line 5; existing format is `Results.ValidationProblem` with an `errors` map by field (repo/src/Microservices/Common/ClassifiedAds.Infrastructure/Web/MinimalApis/ModelValidationFilter.cs, used through `ModelValidationFilter<ProductModel>` in ClassifiedAds.WebAPI/Endpoints/ProductsEndpoints.cs). Create reply 201 — existing behaviour, ProductsEndpoints.cs and repo/eval/smoke.sh. "Nothing stored on 400" follows from "refuse" in AC2.
- R2 (AC1, AC2, AC3): the same for update; update reply 200 — existing behaviour, ProductsEndpoints.cs and repo/eval/smoke.sh. Product PD002 "Galaxy S24" — ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs.
- R3 (AC1): existing longer names stay until somebody edits them — answers.md line 4. An edit that keeps a longer name is refused — follows from AC2 (update refuses a longer name). 120 letters is test data.

## Gaps

- No source says if white space at the start or end of the name counts in the 100 characters; left out.
- No source gives the text of the validation message; scenarios check only the field.
- Characters outside plain letters (emoji, combined accents) are out of scope "for now" (answers.md line 3); no scenario.
