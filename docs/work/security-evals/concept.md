# Concept: security eval cases from a .NET port of OWASP Juice Shop

Date: 2026-11-23. Status: approved on 2026-11-23. The owner accepted each recommendation.

The security skills have few hard eval cases. `secure-coding` has no hard case, and `web-security` has none. `api-design` has one easy scenario (S7) and one new scenario (S9). This concept adds cases that come from OWASP Juice Shop. Juice Shop is an app with intentional security defects for training. Each defect has a known fix.

## Contents

- Source
- What the probe showed
- The port
- Case types
- Cases
- Measure
- Safety
- Files
- Decision sheet
- Cost

## Source

- OWASP Juice Shop, commit `c9f9922` of 2026-11-19 (version 20.4.0-SNAPSHOT). MIT license.
- The back end is Node.js with Express. The front end is Angular 22, the version of `frontend-stack:angular`.
- The folder `data/static/codefixes/` has 37 coding challenges. Each challenge has the vulnerable code, 3 or 4 candidate fixes, and an `info.yml` file. The `info.yml` file tells why each fix is correct or wrong. One fix of each challenge has the suffix `_correct`.
- `swagger.yml` is the OpenAPI 3.0 contract of the B2B order API.
- No ASP.NET Core port of Juice Shop exists. The nearest .NET apps are AspGoat (small labs, not Juice Shop), WebGoat.NET ports (archived), and BadApi (no license).

## What the probe showed

No probe ran yet. We know two risks before the first run:

- **Memory.** Models know Juice Shop well. Its solutions are public (`SOLUTIONS.md`, many blog posts). The name, the user names (Bender, Jim, Bjoern), and the `vuln-code-snippet` markers tell the model the answer. The cases remove them.
- **Ceiling.** The Juice Shop team made the codefix challenges for training. A strong model can pass them without the skill. The earlier review cases of dev-stack were at the ceiling for the same reason. The hard cases add defects in existing code, quiet defaults, and decoys.

## The port

The port is a small shop service with a neutral name, for example `Kiosk`. It is not a copy of the full Juice Shop.

- Back end: ASP.NET Core 10 minimal APIs, EF Core 10 on PostgreSQL (Npgsql), JWT and cookie sign-in, a SignalR hub, and a worker host. About 80 to 100 source files. The tests start PostgreSQL with Testcontainers.
- Front end: the Angular 22 components of Juice Shop that hold the browser defects, with the rest of the UI removed.
- The port keeps the behaviour of each defect, not its JavaScript form. A Juice Shop SQL string in Sequelize becomes a SQL string in `FromSqlRaw`.
- The start point builds, and its tests pass.

Defects of the port, with the Juice Shop challenge and the skill:

| No | Defect | Challenge | Skill |
|----|--------|-----------|-------|
| K1 | Sign-in builds SQL from the email text | `loginAdmin`, `loginJim`, `loginBender` | `secure-coding` |
| K2 | Product search builds SQL from the query text | `unionSqlInjection`, `dbSchema` | `secure-coding` |
| K3 | A user reads the basket of another user | basket access (no codefix) | `secure-coding`, `api-design` |
| K4 | Registration accepts `role: admin` in the body | `registerAdmin` | `secure-coding`, `api-design` |
| K5 | A product change needs no sign-in | `changeProduct` | `secure-coding`, `api-design` |
| K6 | A review takes its author from the body | `forgedReview` | `secure-coding` |
| K7 | A review update changes each review that matches a filter from the body | `noSqlReviews` | `secure-coding` |
| K8 | Password reset with a security question, no rate limit, weak passwords accepted | `resetPassword*`, `weakPassword` | `secure-coding` |
| K9 | The chatbot puts user text in the system prompt, and has no limit on a gift | `chatbotPromptInjection`, `chatbotGreedyInjection` | `secure-coding` |
| K10 | Metrics, access logs, and a file folder are open to each user | `exposedMetrics`, `accessLogDisclosure`, `directoryListing` | `secure-coding` |
| K11 | A redirect allowlist uses a substring test | `redirect`, `redirectCryptoCurrency` | `secure-coding` |
| K12 | A key in a Terraform file, and an old base image in the Dockerfile | `iacLeakedKey`, `vulnerableDockerImage` | `secure-coding` |
| W1 | Product descriptions go through `bypassSecurityTrustHtml` | `restfulXss`, `xssBonus` | `web-security` |
| W2 | The search term goes into the DOM without the sanitizer | `localXss` | `web-security` |
| W3 | The admin route has only a client-side check | `adminSection`, `scoreBoard` | `web-security` |
| W4 | The JWT is in `localStorage` | Juice Shop design | `web-security` |

### ASP.NET Core defects

Change of 2026-11-23, approved by the owner. These defects have a real form in ASP.NET Core 10. Each one gets an exploit test, except N24.

| No | Defect | Exploit test | Juice Shop parallel |
|----|--------|--------------|---------------------|
| N1 | The JWT signing key is in `appsettings.json`, and `ValidateIssuerSigningKey` is `false` | A forged admin token gets access | Forged and unsigned JWT |
| N2 | `FromSqlRaw($"... {term}")`. The interpolation does not make a parameter | A UNION query reads the user table | `unionSqlInjection` |
| N3 | `JsonPatchDocument<User>.ApplyTo` on the entity | A patch of `/role` makes the user an admin | `registerAdmin` |
| N4 | An endpoint returns the EF `User` entity | The JSON shows `PasswordHash` and `TotpSecret` | Exposed password hashes |
| N5 | The owner check reads the `sub` claim, but JwtBearer maps it to `NameIdentifier`. Guest orders have no owner, so `null == null` passes | A user reads each guest order | Basket of another user |
| N6 | One endpoint of a `MapGroup` has no `RequireAuthorization`, and no fallback policy exists | An anonymous call runs an admin action | `adminSection` |
| N7 | `Redirect(returnUrl)` after a `StartsWith("/")` check | `//evil.example` and `/\evil.example` redirect away | `redirect` |
| N8 | `Path.Combine(root, name)` for invoice downloads | An absolute path reads a file outside the root | FTP folder, poison null byte |
| N9 | An upload keeps the file name of the user in `wwwroot` and checks only `Content-Type` | An `.html` or `.svg` file gives stored XSS | Upload type |
| N10 | B2B XML import with `DtdProcessing.Parse` and an `XmlUrlResolver` | XXE reads a local file | XXE data access |
| N11 | B2B JSON with Newtonsoft `TypeNameHandling.Auto` | A `$type` runs a gadget class of the app | B2B remote code execution |
| N12 | A profile image from a URL, with no allowlist | SSRF reaches an internal admin endpoint | SSRF |
| N13 | Cookie sign-in, a form endpoint with `DisableAntiforgery`, and `SameSite=None` | A cross-site form changes the email | CSRF |
| N14 | CORS with `SetIsOriginAllowed(_ => true)` and `AllowCredentials` | A foreign origin reads user data | none |
| N15 | `ForwardedHeaders` with `KnownProxies` and `KnownNetworks` cleared | `X-Forwarded-For: 127.0.0.1` passes the IP check of `/metrics` and the login rate limit | `exposedMetrics` |
| N16 | An output-cache policy that also stores responses for signed-in users | User B gets the basket of user A | none |
| N17 | The developer exception page, or `ex.Message` in `ProblemDetails`, in Production | A SQL error shows the query | Error handling |
| N18 | Unsalted MD5 passwords, a reset token from `Random`, and a compare with `==` | The test cracks a hash and predicts a token | `weakPassword`, reset |
| N19 | The reset link uses `Request.Host`, and `AllowedHosts` is `*` | A forged `Host` header sends the link to the attacker | none |
| N20 | `HttpLogging` with the request body | The log shows the password | `accessLogDisclosure` |
| N21 | The Data Protection key ring is in the repository | A forged auth cookie gets access | none |
| N22 | A SignalR support hub with no `[Authorize]` and group names from the client | A user reads the chat of another customer | none |
| N23 | No server validation of order quantity and review rating | A negative quantity gives a negative total. The API stores a rating of 0 | Payback time, zero stars |
| N24 | The Dockerfile uses `aspnet:10.0.0-rc.1` (CVE-2025-55315), `USER root`, Development mode, and a secret in `ENV`. Newtonsoft.Json 12.0.3 with `NuGetAuditSuppress` | No exploit. The review case checks it | `vulnerableDockerImage` |

The build agent checks each version and advisory against `dotnet/announcements` and the GitHub advisory database before it uses it. Out of scope:

- ReDoS. We did not verify the timeout defaults.
- Request size limits. A DoS test is not safe on the test machine.
- Timing attacks. No stable test exists.

Decoys of the port (code that looks wrong but is correct), about 8:

- An interpolated `FromSql($"... {email}")`. EF Core makes this a parameter. It is near N2.
- An admin endpoint with a server-side policy, next to the client-side guard.
- `bypassSecurityTrustResourceUrl` on a fixed, allowlisted video URL.
- A secret name in `appsettings.json` with an empty value and a user-secrets note.
- `LocalRedirect(returnUrl)` in a second sign-in flow, near N7.
- An `XmlReader` with the default settings in a second import, near N10.
- A `ForwardedHeaders` setup with one known proxy, in the worker host, near N15.
- An output-cache policy on the public product list only, near N16.

## Case types

### Codefix choice

The agent gets the vulnerable code of one challenge and its candidate fixes, with the names removed and the order shuffled. It picks one fix and gives the reason in at most 3 sentences.

- Score: the pick is correct or not. A script checks it against the `_correct` file. No judge is necessary.
- The code stays in TypeScript. The skill rules are about the defect, and the choice does not need a build.
- 27 challenges fit the 3 skills: 22 for `secure-coding` and 5 for `web-security`. The 5 web3 and NFT challenges and `tokenSale` are out of scope.

### Hard fix case

The agent gets the port and a business request that touches the defects. It does not name a defect. Hidden exploit tests score the result: each test runs the attack against the changed code. A judge scores the report.

### Hard review case

The agent reviews a branch of the port. The branch adds a feature and keeps 8 to 10 defects. The answer sheet lists the planted defects and the decoys. A finding on a decoy is a false positive.

## Cases

| Case | Skill | Type | Defects |
|------|-------|------|---------|
| `secure-coding-codefix` | `secure-coding` | Codefix choice, 22 challenges | K1 to K12 |
| `secure-coding-hard-injection` | `secure-coding` | Fix: add a product filter and an email sign-in for staff | K1, K2, K7, K11 |
| `secure-coding-hard-access` | `secure-coding` | Fix: add a shared basket and product reviews by staff | K3, K4, K5, K6, K10, N3 to N6, N16 |
| `secure-coding-hard-auth` | `secure-coding` | Fix: add a password reset by mail and chatbot coupons | K8, K9, K12, N1, N18, N19, N21 |
| `secure-coding-hard-platform` | `secure-coding` | Fix: add invoice downloads, a B2B XML and JSON import, and profile images from a URL | N7 to N15, N17, N20, N22, N23 |
| `secure-coding-review-hard` | `secure-coding` | Review of a branch with a wish list | 10 of K1 to K12 and N1 to N24, 8 decoys |
| `web-security-codefix` | `web-security` | Codefix choice, 5 challenges | W1 to W3 |
| `web-security-hard` | `web-security` | Fix: show rich product descriptions and an admin page | W1 to W4 |
| `web-security-review-hard` | `web-security` | Review of a branch with a product comparison page | W1 to W4, 3 decoys |
| `api-design-review-juice` | `api-design` | Review of the contract of the port, and of the Juice Shop `swagger.yml` | K3, K4, K5, rate limit, errors, OpenAPI 3.0 |

## Stage 2: other skills on the same port

Change of 2026-11-23, asked by the owner. The port is a realistic service, so it can also hold the hidden traps of other stack skills. Stage 2 starts after the security cases have results.

| Skill | Examples of traps in the port |
|-------|-------------------------------|
| `efcore` | A lost update on stock (no concurrency token). N+1 queries in the order list. Tracked queries in read endpoints. A migration that drops a column with data |
| `postgresql` | A daily report that counts UTC days, a job queue without `SKIP LOCKED`, a unique email that is case-sensitive, a missing index on a foreign key |
| `aspnetcore` | A captive dependency (scoped service in a singleton), a wrong middleware order |
| `fluentvalidation` | Rules for N23, with stable error codes |
| `dotnet-observability` | N20, personal data in logs and traces |
| `redis` | N16, per-user data in a shared cache |
| `mailkit` | The reset mail of N19 |
| `api-design` | The contract of the port |
| `dotnet-testing` | Tests that prove each fix |

Each stage 2 case follows the hard-case rules of dev-stack. It has a business request and 6 traps or more, with 2 in existing code and 2 quiet defaults. Probe tests check the traps. The build of stage 1 keeps a clean module structure, so that stage 2 can add traps without a rewrite.

## Measure

- Each case runs 3 times without the skill and 3 times with it.
- Models: Haiku 5.5 and Sonnet 5.5. In the earlier rounds, the gain was largest on these models.
- A hard case is good when the baseline fails 3 or more items in 2 or more of its runs. This is the rule of the dev-stack hard cases.
- The codefix cases run first. They are cheap and show the ceiling before the build of the port.

## Safety

- The port is a test fixture with intentional defects. It never runs on a public address. The exploit tests use `WebApplicationFactory` in the test process.
- The leaked key of K12 is a fake value with a clear test prefix, so that secret scanning gives no false alert.
- The Dockerfile of K12 is text for the review. No step builds or pulls its image.
- The fixture folder has the Juice Shop MIT notice and a README that names it as an eval fixture with intentional defects.

## Files

| Path | Content |
|------|---------|
| `evals/dev-stack/fixtures/kiosk/` | The port: back end, front end, tests |
| `evals/dev-stack/fixtures/kiosk/LICENSE-juice-shop` | The MIT notice of Juice Shop |
| `evals/dev-stack/cases/secure-coding-*.json`, `web-security-*.json`, `api-design-review-juice.json` | The case objects |
| `evals/dev-stack/probes/kiosk-*/` | The hidden exploit tests |
| `evals/dev-stack/planted/*-review-hard.json` | Planted defects and decoys |
| `evals/dev-stack/selftest/kiosk-*/` | The `naive` and `good` overlays |
| `evals/security/codefix/` | The anonymized challenges, the answer key, and `codefix.py` |
| `docs/work/security-evals/results.md` | The results |

The cases use the runner, scorer, and grader of `evals/dev-stack` (`run.py`, `score.py`, `grade.py`), as the `angular` cases do.

## Decision sheet

The recommendation is the answer unless the owner changes the row.

| ID | Question | Recommendation |
|----|----------|----------------|
| SE-01 | Port to .NET or keep TypeScript? | Port the back end to ASP.NET Core 10 for the hard cases. Keep TypeScript for the codefix choice. |
| SE-02 | Hard cases, codefix choice, or both? | Both. The owner chose this. |
| SE-03 | Which skills? | `secure-coding`, `web-security`, and `api-design`. The owner chose this. |
| SE-04 | Name of the port? | A neutral name such as `Kiosk`. No Juice Shop names, user names, or challenge names in the code or the task. |
| SE-05 | Where do the cases live? | In `evals/dev-stack`, with its runner. The codefix choice gets its own small script in `evals/security/codefix/`. |
| SE-06 | Models and runs? | Haiku 5.5 and Sonnet 5.5, 3 runs for each variant. |
| SE-07 | Order of the work? | 1. The codefix choice. 2. The port with its self-test. 3. The hard cases. A stop after step 1 when the baseline is at the ceiling on all challenges. |
| SE-08 | Front end of the port? | The Juice Shop Angular 22 components with W1 to W4, with the rest of the UI removed. |
| SE-10 | Database of the port? | PostgreSQL, so that `efcore` and `postgresql` cases can use the same port (stage 2). |
| SE-09 | Release? | No plugin release. The cases are eval files. A skill change that the results cause gets its own PR and release. |

## Cost

| Step | Estimate |
|------|----------|
| Codefix choice: 27 challenges, 2 variants, 2 models, 3 runs | About 324 short runs, $10 to $15 |
| The port, the probes, and the self-test | $150 to $200 of agent work |
| The hard cases: 8 cases, 2 variants, 2 models, 3 runs | 96 runs and their grades, $35 to $60 |

Cap: $300. At most 4 agents at the same time. One `dotnet build` at a time.
