
## gap (18)

| Case | Item | Baseline | Skill | What it tests |
|------|------|----------|-------|---------------|
| dotnet-observability-review-hard | J2 | 0/3 | 0/3 | The review finds every planted mistake with severity 'high' (P1, P4). |
| dotnet-observability-review-hard | J5 | 0/3 | 0/3 | For P1, the review says that SamplingRatio = 1.0F alone does not keep all traces, because the distro default TracesPerSecond (5) wins, and r |
| dotnet-observability-review-hard | P1 | 0/3 | 0/3 | Version fact, existing code next to the change: the change edits the telemetry block but keeps options.SamplingRatio = 1.0F alone under the  |
| elasticsearch-review-hard | J2 | 0/3 | 0/3 | The review finds every planted mistake with severity 'high' (P1, P2, P3). |
| elasticsearch-review-hard | J5 | 0/3 | 0/3 | For P1, the review says that the RemoveIndex and Add calls on one descriptor send only one action (the alias add), so the swap fails while t |
| elasticsearch-review-hard | P1 | 0/3 | 0/3 | Client fact: in Elastic.Clients.Elasticsearch 9.5.3 one IndexUpdateAliasesActionDescriptor holds one action. RemoveIndex and then Add on the |
| fluentvalidation-hard | J4 | 0/3 | 0/3 | The final message lists the error codes that the webshop can get from POST /orders, and how they reach the response (field and code). |
| fluentvalidation-review-hard | P6 | 0/3 | 0/3 | ClassLevelCascadeMode = CascadeMode.Stop stops the whole validator after the first rule that fails, not each field. The customer gets one er |
| mailkit-hard | J1 | 0/3 | 0/3 | The final message says that the fixture suppressed known advisories of MailKit 4.15.0 and MimeKit 4.15.0, and that the reason of the suppres |
| postgresql-hard | C7 | 0/3 | 0/3 | The existing daily report counts UTC days (::date in the session time zone). |
| puppeteersharp-hard | C5 | 0/3 | 0/3 | PdfOptions without Format prints Letter; DOC-01 asks for A4. |
| puppeteersharp-hard | C6 | 0/3 | 0/3 | PrintBackground is false by default: the header bar and the row colors are lost. |
| puppeteersharp-hard | J6 | 0/3 | 0/3 | The final message reports the existing defects of the packing slip that the work fixed or left open: at least the Letter page size against D |
| sqlserver-hard | C10 | 0/3 | 0/3 | The existing sales report counts UTC days. |
| sqlserver-hard | C11 | 0/3 | 0/3 | An interpolated string on a varchar column sends nvarchar(4000): CONVERT_IMPLICIT and an index scan (existing reservation and receipt SQL, o |
| sqlserver-hard | J4 | 0/3 | 0/3 | The final message says that Guid.CreateVersion7 is not ordered in SQL Server (so the existing Shipment key and the wording of DATA-07 do not |
| wolverine-review-hard | J2 | 0/3 | 0/3 | The review finds every planted mistake with severity 'high' (P1, P2, P8). |
| wolverine-review-hard | P2 | 0/3 | 0/3 | Version fact, over two files: the handler injects ICarrierClient, which Program.cs (line 54) registers with AddHttpClient<ICarrierClient, Ht |

## worse (21)

| Case | Item | Baseline | Skill | What it tests |
|------|------|----------|-------|---------------|
| dotnet-observability-review-hard | P3 | 3/3 | 2/3 | Over two files: the change registers the meter Orders.Carriers and the source Orders.Shipping, but not the meter Orders.Shipping of Shipping |
| elasticsearch-review-hard | J4 | 3/3 | 2/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| elasticsearch-review-hard | P9 | 3/3 | 2/3 | The new stock test waits with fixed Task.Delay calls of 2 seconds and then searches once. On a slow CI machine the worker is not done and th |
| elasticsearch-review-hard | D4 | 3/3 | 2/3 | Decoy, must not be flagged: The outbox rows for a stock change are added inside the SaveChangesAsync override before base.SaveChangesAsync,  |
| postgresql-review-hard | J5 | 1/3 | 0/3 | For P4, the review says that the check constraint is added without NOT VALID, so PostgreSQL scans order_lines under an ACCESS EXCLUSIVE lock |
| puppeteersharp-hard | C2 | 3/3 | 0/3 | tests_pass (generic) |
| puppeteersharp-hard | C4 | 1/3 | 0/3 | probe_tests |
| puppeteersharp-hard | C8 | 1/3 | 0/3 | Our logo is loaded from the CDN on every render. |
| puppeteersharp-review-hard | D1 | 2/3 | 1/3 | Decoy, must not be flagged: catch (TimeoutException) is correct here: in PuppeteerSharp 25.14.0, SetContentAsync and PdfDataAsync throw Syst |
| rabbitmq-review-hard | J4 | 3/3 | 2/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| rabbitmq-review-hard | D3 | 3/3 | 2/3 | Decoy, must not be flagged: The unique index on the nullable ProviderReference is correct. PostgreSQL treats NULL values as distinct in a un |
| redis-hard | J2 | 2/3 | 1/3 | The final message says that checkout read the price through the product cache (existing code), and states how checkout now gets the current  |
| redis-review-hard | J2 | 3/3 | 1/3 | The review finds every planted mistake with severity 'high' (P3, P4, P5). |
| redis-review-hard | P3 | 3/3 | 1/3 | Existing code that the change now depends on (the new comment at line 60): checkout reads the price through ProductCache, which keeps an L1  |
| sqlserver-hard | C2 | 2/3 | 1/3 | tests_pass (generic) |
| wolverine-hard | C12 | 3/3 | 2/3 | AutoProvision declares the warehouse-owned quorum queue with Wolverine's dead-letter argument: PRECONDITION_FAILED and broker start retries. |
| wolverine-hard | J4 | 3/3 | 2/3 | The final message says how the generated Wolverine code of the new or changed handlers reaches production (codegen write, the files in Inter |
| wolverine-hard | J5 | 2/3 | 0/3 | The final message marks the open team decisions as assumed: at least the retry policy (MSG-08) and the handling of business duplicates (MSG- |
| wolverine-review-hard | J4 | 2/3 | 0/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| wolverine-review-hard | P9 | 3/3 | 2/3 | The test that should prove the retry of a platform outage uses InvokeMessageAndWaitAsync. Invoke runs the handler inline and throws to the c |
| wolverine-review-hard | D5 | 2/3 | 0/3 | Decoy, must not be flagged: Publisher confirms without UseQuorumQueues() is correct here: the existing queues (orders-api.payment-captured,  |

## chance (39)

| Case | Item | Baseline | Skill | What it tests |
|------|------|----------|-------|---------------|
| aspnetcore-openapi-hard | C8 | 0/3 | 2/3 | An IResult file response without a typed Produces has no content in the document. |
| aspnetcore-openapi-hard | J3 | 0/3 | 2/3 | The final message says that the build starts the app to write the document, and that the startup migration is skipped only for the document  |
| aspnetcore-openapi-review-hard | J3 | 2/3 | 2/3 | The review flags none of the decoys: it does not call a decoy a problem and does not ask to change it. |
| aspnetcore-openapi-review-hard | P4 | 1/3 | 2/3 | POST /returns documents 409, but the handler answers 422 (Problems.UnprocessableEntity, line 47) when the return policy refuses the return;  |
| aspnetcore-openapi-review-hard | P5 | 2/3 | 2/3 | The Idempotency-Key header is documented with Required = false, but IdempotencyFilter rejects a request without the header with 400. The gen |
| aspnetcore-openapi-review-hard | D1 | 2/3 | 2/3 | Decoy, must not be flagged: GET /customers/{id} has no ProducesProblem. Correct: the handler returns Results<Ok<CustomerResponse>, NotFound> |
| dotnet-observability-hard | C9 | 0/3 | 1/3 | The existing orders.customer_id tag, or an id on a new booking metric. |
| dotnet-observability-hard | C10 | 0/3 | 1/3 | SamplingRatio = 1.0F alone keeps 5 traces per second (TracesPerSecond default, Azure Monitor distro 1.5.0 and later). |
| dotnet-observability-hard | J1 | 0/3 | 1/3 | The final message says that the existing sampling setting did not keep all traces: with the Azure Monitor distro, TracesPerSecond (default 5 |
| dotnet-observability-review-hard | P2 | 0/3 | 2/3 | Over two files: CarrierLog.cs (line 3) relies on the dispatch scope to add ShipmentId to the refusal log, and the Refused event has no shipm |
| elasticsearch-hard | J4 | 1/3 | 2/3 | The final message names at least two problems of the existing search code that the change touches, and says what it did with each: the brand |
| elasticsearch-hard | J5 | 0/3 | 2/3 | The final message marks the open team decisions as assumed: at least the analyzers for Dutch and German text (SEARCH-06) and the index name  |
| elasticsearch-review-hard | P7 | 0/3 | 2/3 | Existing code next to the change: the search response is used without a check of IsValidResponse (the new EnsureValid helper is not used her |
| fluentvalidation-hard | C8 | 1/3 | 1/3 | No cascade: Must and the catalog get null. SetValidator skips a null item, and the handler throws. |
| mailkit-review-hard | P7 | 0/3 | 2/3 | Existing code that the change touches: the return confirmation puts the customer comment (and the customer name, line 24) into the HTML body |
| postgresql-review-hard | J4 | 0/3 | 2/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| postgresql-review-hard | D2 | 0/3 | 2/3 | Decoy, must not be flagged: HasDefaultValueSql("uuidv7()") is correct on PostgreSQL 18, which has the built-in function uuidv7() (checked on |
| postgresql-review-hard | D4 | 2/3 | 2/3 | Decoy, must not be flagged: IsCreatedConcurrently on the picking index: EF Core (Npgsql) runs CREATE INDEX CONCURRENTLY outside the migratio |
| puppeteersharp-review-hard | J4 | 1/3 | 1/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| puppeteersharp-review-hard | P5 | 0/3 | 2/3 | Quiet default in the rewritten PdfOptions: PrintBackground is not set and is false by default, so the dark blue header bar and the row color |
| rabbitmq-hard | C10 | 0/3 | 2/3 | Existing consumer: the nack loop of the base class on RabbitMQ 4.3. |
| rabbitmq-hard | C12 | 0/3 | 2/3 | No BasicQos: prefetch 0 (no limit). |
| rabbitmq-review-hard | P10 | 0/3 | 1/3 | No BasicQosAsync on the consumer channel: the prefetch is 0 (no limit), and the broker pushes every message of the queue to one consumer. Se |
| redis-hard | C4 | 0/3 | 1/3 | The cache package alone resolves StackExchange.Redis 2.7.27. |
| redis-hard | J4 | 1/3 | 1/3 | The final message gives the reason for the new cache key (or key version): the shape of the cached value changed, and entries of the old sha |
| redis-hard | J5 | 0/3 | 2/3 | The final message marks the open team decisions as assumed: at least how all instances see a change (CACHE-07), the TTLs (CACHE-04), and the |
| redis-review-hard | J4 | 1/3 | 1/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| redis-review-hard | P6 | 0/3 | 2/3 | Existing key without a version segment, and the change adds ListPrice and Promotion to the cached ProductResponse. During the rolling deploy |
| redis-review-hard | D2 | 1/3 | 1/3 | Decoy, must not be flagged: A lock expiry of 2 minutes with a 1 minute timer is correct: the lock only prevents duplicate work, the release  |
| sqlserver-hard | C5 | 2/3 | 2/3 | probe_tests |
| sqlserver-hard | C6 | 2/3 | 2/3 | A tracked read, a check in C# and SaveChanges: more orders than maxUses get the voucher. |
| sqlserver-hard | C7 | 2/3 | 2/3 | Check-then-insert for 'once by each customer', or 2627 not mapped to 409 with a rollback. |
| sqlserver-hard | C8 | 2/3 | 2/3 | MERGE without HOLDLOCK (copied from the receipt endpoint), or check-then-insert: 2627 on a repeated call. |
| sqlserver-hard | C9 | 2/3 | 2/3 | AT TIME ZONE on the datetime2 column reads UTC as Amsterdam time (copied from the sales report). |
| sqlserver-hard | J2 | 0/3 | 2/3 | The existing receipt upsert (MERGE without HOLDLOCK in InventoryEndpoints) gets HOLDLOCK, or the final message reports it as an open point. |
| sqlserver-review-hard | P10 | 1/3 | 2/3 | Existing code next to the change: the change edits this file and makes the stock count MERGE use HOLDLOCK because receipts run at the same t |
| wolverine-review-hard | J1 | 2/3 | 2/3 | The review finds at least 7 of the 9 planted mistakes. |
| wolverine-review-hard | J5 | 0/3 | 2/3 | For P1, the review says that Wolverine 6 does not discover IForwardsTo by itself and recommends opts.RegisterMessageForwarder<PaymentCapture |
| wolverine-review-hard | P1 | 0/3 | 2/3 | Version fact, over two files: the handler now takes PaymentCapturedV2 only, and the comment (line 11) relies on IForwardsTo in the contract. |

## skill-win (119)

| Case | Item | Baseline | Skill | What it tests |
|------|------|----------|-------|---------------|
| aspnetcore-openapi-hard | C5 | 0/3 | 3/3 | The automatic 400 of AddValidation is not in the document. |
| aspnetcore-openapi-hard | C6 | 0/3 | 3/3 | The header and the 409 of an endpoint filter are not in the document. |
| aspnetcore-openapi-hard | C7 | 0/3 | 3/3 | TypedResults.Problem in a Results union is not in the document. |
| aspnetcore-openapi-hard | C11 | 2/3 | 3/3 | No operationId on the existing endpoints, or the document is public in Production. |
| aspnetcore-openapi-hard | J1 | 1/3 | 3/3 | The final message asks decision API-10 (versioning for an external consumer) with a recommendation, or marks it as assumed. |
| aspnetcore-openapi-hard | J2 | 0/3 | 3/3 | The final message asks decision API-11 (the JSON number and enum settings that make an int ["integer","string"] in the schema), or marks it  |
| aspnetcore-openapi-hard | J4 | 0/3 | 3/3 | The final message says that the document file must be committed after each contract change, and how a pull request or CI shows a change (a d |
| aspnetcore-openapi-review-hard | J1 | 2/3 | 3/3 | The review finds at least 7 of the 10 planted mistakes. |
| aspnetcore-openapi-review-hard | J7 | 2/3 | 3/3 | The fix for the startup migration keeps the migration on normal starts in Production and skips it only when the build makes the document (en |
| aspnetcore-openapi-review-hard | P3 | 2/3 | 3/3 | Produces(200, contentType: "application/pdf") without a response type writes a 200 with no content in the document (checked: the committed d |
| aspnetcore-openapi-review-hard | P6 | 1/3 | 3/3 | GenerateDocumentationFile is on and the comment says that the XML comments become summaries, but every handler is a private static method. T |
| aspnetcore-openapi-review-hard | P7 | 2/3 | 3/3 | Produces<ProblemDetails>(404) documents the 404 as application/json, but the handler returns TypedResults.Problem with application/problem+j |
| aspnetcore-openapi-review-hard | P10 | 1/3 | 3/3 | POST /customers is validated by AddValidation (DataAnnotations on CreateCustomerRequest), but the automatic 400 is not in the document. POST |
| dotnet-observability-hard | J4 | 2/3 | 3/3 | The existing counter orders.shipments.dispatched no longer has a customer id tag, or the final message reports the tag as an open point (hig |
| dotnet-observability-hard | J5 | 1/3 | 3/3 | The final message reports that the existing refusal log wrote the gateway answer with the personal data of the recipient, and that the new l |
| dotnet-observability-hard | J6 | 2/3 | 3/3 | A refused or failed booking is logged one time, with the exception as the exception argument where an exception is logged. The dispatch no l |
| dotnet-observability-hard | J7 | 1/3 | 3/3 | The final message marks the open team decisions as assumed: at least the logging style or pipeline (OPS-23) and the data classification of t |
| dotnet-observability-review-hard | J1 | 2/3 | 3/3 | The review finds at least 7 of the 10 planted mistakes. |
| dotnet-observability-review-hard | P7 | 0/3 | 3/3 | The exception is passed as ex.Message in a string parameter (CarrierLog.cs line 14), not as the exception argument. The log loses the except |
| dotnet-observability-review-hard | P8 | 2/3 | 3/3 | A failed booking is logged two times: the gateway already logs it (the warning on line 63 or GatewayUnreachable on line 80 of HttpCarrierGat |
| elasticsearch-hard | C4 | 0/3 | 3/3 | The existing Refresh.True on each bulk call stays, or new code adds query_string, a larger max_result_window, or TLS off. |
| elasticsearch-hard | C7 | 1/3 | 3/3 | The existing brand filter is a term query on a text field. |
| elasticsearch-hard | C9 | 0/3 | 3/3 | The unchecked search response hides the 400 of a page beyond 10,000 hits as an empty page. |
| elasticsearch-hard | C10 | 1/3 | 3/3 | The existing worker deletes outbox rows of failed bulk items (429 while the index is blocked). |
| elasticsearch-hard | C11 | 1/3 | 3/3 | The new mapping goes only into the create-if-missing code, so the live index keeps the old mapping and rejects the new fields. |
| elasticsearch-hard | J3 | 0/3 | 3/3 | The search puts the non-scoring clauses (active, stock, brand, category) in bool.filter, sends the user text through match, multi_match or s |
| elasticsearch-review-hard | J1 | 1/3 | 3/3 | The review finds at least 7 of the 9 planted mistakes. |
| elasticsearch-review-hard | J3 | 1/3 | 3/3 | The review finds at least 3 of the 5 planted mistakes marked 'subtle' (P1, P2, P4, P5, P7). |
| elasticsearch-review-hard | J6 | 2/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| elasticsearch-review-hard | P5 | 1/3 | 3/3 | Existing code that the change keeps: IndexAsync checks only the HTTP status of the bulk call, and the worker then deletes the outbox rows. A |
| fluentvalidation-hard | C9 | 0/3 | 3/3 | The existing ValidationFilter has no null check. In Production a JSON null body reaches it as null. |
| fluentvalidation-hard | C10 | 2/3 | 3/3 | The existing import calls the sync Validate on a validator that now has a MustAsync rule. |
| fluentvalidation-hard | J1 | 1/3 | 3/3 | The final message says that the existing CSV import called the sync Validate and that it would fail (a 500) once the validator has an async  |
| fluentvalidation-review-hard | J3 | 1/3 | 3/3 | The review flags none of the decoys (D1 to D4) as a problem and asks to change none of them. |
| fluentvalidation-review-hard | P8 | 0/3 | 3/3 | Existing code next to the change: the filter takes the argument with .Single() and has no null check. The change puts the filter on POST and |
| fluentvalidation-review-hard | D1 | 1/3 | 3/3 | Decoy, must not be flagged: RuleFor(x => x.Address!).SetValidator(addressValidator) without NotNull() is correct here: on update the address |
| mailkit-hard | C4 | 0/3 | 3/3 | MailKit 4.15.0 and MimeKit 4.15.0 have advisories that the fixture suppresses with NuGetAuditSuppress. |
| mailkit-hard | C5 | 0/3 | 3/3 | CVE-2026-41319 (MailKit below 4.16.0) and CVE-2026-30227 (MimeKit 4.15.0 and earlier). |
| mailkit-hard | C6 | 0/3 | 3/3 | Suppressed advisories stay, or the TLS downgrade of the existing settings stays. |
| mailkit-hard | C8 | 0/3 | 3/3 | The existing pattern sends inline and swallows the error: the notice is lost while the relay is down. |
| mailkit-hard | C11 | 0/3 | 3/3 | The existing StartTlsWhenAvailable setting sends AUTH in plain text. |
| mailkit-hard | J2 | 0/3 | 3/3 | The existing return confirmation no longer loses its mail when the relay is down (it uses the same send path as the shipping notice), or the |
| mailkit-hard | J3 | 2/3 | 3/3 | The existing return confirmation encodes the customer name and the comment in the HTML body, or the final message reports the missing encodi |
| mailkit-hard | J4 | 0/3 | 3/3 | The sender creates one SmtpClient for each batch or each send (not a singleton), sets a timeout, passes the CancellationToken, and calls Dis |
| mailkit-hard | J5 | 0/3 | 3/3 | The final message marks the open team decisions as assumed: at least the send path (MAIL-02) and the mail template (MAIL-03). |
| mailkit-review-hard | J1 | 1/3 | 3/3 | The review finds at least 7 of the 9 planted mistakes. |
| mailkit-review-hard | J2 | 0/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P2, P3). |
| mailkit-review-hard | J3 | 1/3 | 3/3 | The review finds at least 3 of the 5 planted mistakes marked 'subtle' (P1, P2, P5, P6, P7). |
| mailkit-review-hard | J5 | 0/3 | 3/3 | For P1, the review says that MailKit 4.15.1 is still affected by GHSA-9j88-vvj5-vhgr (CVE-2026-41319, fixed in 4.16.0) and recommends an upg |
| mailkit-review-hard | P1 | 0/3 | 3/3 | Version fact, over two files: the change upgrades MailKit to 4.15.1 (Directory.Packages.props line 10) and keeps the suppression of GHSA-9j8 |
| mailkit-review-hard | P2 | 1/3 | 3/3 | Existing code next to the change: the new outbox sender connects with smtp.Security, and the default (and appsettings.json line 19) is Start |
| postgresql-hard | C6 | 0/3 | 3/3 | The existing claim: SKIP LOCKED in autocommit, then a second statement. Two workers get the same order. |
| postgresql-hard | C8 | 2/3 | 3/3 | date_trunc('week', timestamptz) uses the session time zone, not Amsterdam. |
| postgresql-hard | C9 | 1/3 | 3/3 | Existing orders get no code, the same code, or an empty code. |
| postgresql-hard | C11 | 2/3 | 3/3 | A plain CREATE INDEX on orders blocks writes for the whole build. |
| postgresql-hard | J1 | 0/3 | 3/3 | The final message reports that the existing picking query lost its row lock at the end of the SELECT (FOR UPDATE SKIP LOCKED in autocommit), |
| postgresql-hard | J2 | 2/3 | 3/3 | The final message reports that the existing daily sales report counted days in UTC (the session time zone) instead of Dutch days, and says t |
| postgresql-hard | J3 | 1/3 | 3/3 | The migration (or the deploy step that the final message describes) sets lock_timeout before the DDL on the orders table. |
| postgresql-hard | J4 | 2/3 | 3/3 | The existing orders get their tracking code in batches with a commit after each batch (or in a separate step outside the migration transacti |
| postgresql-hard | J6 | 1/3 | 3/3 | The final message marks the open team decisions as assumed: at least how DDL on the live orders table is done (DATA-25: lock_timeout, concur |
| postgresql-review-hard | J2 | 1/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P2, P4). |
| postgresql-review-hard | P2 | 1/3 | 3/3 | Existing code that the change edits, over two files: 'SELECT ... FOR UPDATE SKIP LOCKED' runs through SqlQuery in autocommit mode, so the ro |
| postgresql-review-hard | P6 | 2/3 | 3/3 | MERGE is not a safe concurrent upsert. Under READ COMMITTED, two calls for the same customer and SKU (a double click) both see no row, both  |
| postgresql-review-hard | D5 | 2/3 | 3/3 | Decoy, must not be flagged: The timeout job is one conditional UPDATE (ExecuteUpdateAsync with the status in the WHERE clause). It is atomic |
| puppeteersharp-hard | C7 | 1/3 | 3/3 | The customer logo URL reaches Chrome (or an unchecked HttpClient): SSRF to internal addresses. |
| puppeteersharp-hard | C9 | 0/3 | 3/3 | The existing sandbox opt-out stays (SEC-24). |
| puppeteersharp-hard | C10 | 0/3 | 3/3 | The image still runs Chrome as root. |
| puppeteersharp-hard | C11 | 0/3 | 3/3 | The existing runtime download of Chrome, or a browser for each document, stays. |
| puppeteersharp-hard | C12 | 0/3 | 3/3 | Chrome is not installed and pinned at image build time. |
| puppeteersharp-hard | J1 | 0/3 | 3/3 | The renderer keeps one browser per process (a singleton that launches again after a disconnect and closes at shutdown) and uses one browser  |
| puppeteersharp-hard | J2 | 2/3 | 3/3 | Concurrent renders are limited (for example a SemaphoreSlim) by a value from configuration, so a pick wave of 40 slips does not open 40 page |
| puppeteersharp-hard | J3 | 0/3 | 3/3 | The customer logo is downloaded on the server, not by Chrome, with these checks: https, a public address checked on the resolved IP address  |
| puppeteersharp-hard | J4 | 1/3 | 3/3 | The page network is blocked when the HTML holds customer text: LaunchOptions.Allowlist with data: and about:blank, or request interception t |
| puppeteersharp-hard | J5 | 1/3 | 3/3 | Our own logo and the font load nothing from the network at render time (the logo is embedded or inlined, the font is installed or inlined),  |
| puppeteersharp-hard | J7 | 0/3 | 3/3 | The final message marks the open team decisions as assumed: at least the sandbox policy (SEC-24) and the base image of the renderer (OPS-03) |
| puppeteersharp-review-hard | J1 | 0/3 | 3/3 | The review finds at least 8 of the 10 planted mistakes. |
| puppeteersharp-review-hard | J2 | 0/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P2, P3). |
| puppeteersharp-review-hard | J3 | 1/3 | 3/3 | The review finds at least 4 of the 6 planted mistakes marked 'subtle' (P2, P3, P4, P5, P6, P7). |
| puppeteersharp-review-hard | J5 | 0/3 | 3/3 | For P3, the review says that Chrome must be installed at image build time (pinned to the Chrome build of the PuppeteerSharp version) and tha |
| puppeteersharp-review-hard | P3 | 0/3 | 3/3 | Existing code next to the change, over two files: ChromeInstaller stays registered, and the Dockerfile still installs no browser (line 8: 'C |
| puppeteersharp-review-hard | P4 | 1/3 | 3/3 | Version fact: the pragma hides CS0618 on SetContentAsync(string, NavigationOptions). In PuppeteerSharp 25 this overload is obsolete: 'The ne |
| puppeteersharp-review-hard | P10 | 0/3 | 3/3 | Each render opens a page in the default browser context of the shared browser. Cookies, cache and storage are shared between the documents o |
| puppeteersharp-review-hard | D2 | 2/3 | 3/3 | Decoy, must not be flagged: The CancellationToken goes only to _pages.WaitAsync. That is correct: SetContentAsync, NewPageAsync and PdfDataA |
| puppeteersharp-review-hard | D5 | 2/3 | 3/3 | Decoy, must not be flagged: HeadlessMode = HeadlessMode.Shell is correct for chrome-headless-shell in PuppeteerSharp 25. It is not the Node  |
| rabbitmq-hard | C4 | 0/3 | 3/3 | BasicNackAsync with requeue: true does not count toward the delivery-limit of RabbitMQ 4.3: a failing message loops forever. |
| rabbitmq-hard | C8 | 0/3 | 3/3 | Quorum queues dead-letter at-most-once by default; at-least-once needs overflow reject-publish. |
| rabbitmq-hard | C9 | 1/3 | 3/3 | A failing payment result loops forever (nack with requeue on 4.3), or is acked and lost. |
| rabbitmq-hard | J1 | 0/3 | 3/3 | The final message says that the existing RabbitMqConsumer<T> (or StockReceivedConsumer) nacks with requeue: true, and that on RabbitMQ 4.3 s |
| rabbitmq-hard | J3 | 0/3 | 3/3 | The consumer treats a payment result that cannot succeed on a retry (unknown order, a body that cannot be read, an order that cannot be paid |
| rabbitmq-hard | J4 | 0/3 | 3/3 | The final message marks the open team decisions that the work needed as assumed, at least the failure handling and dead letters (MSG-08) and |
| rabbitmq-review-hard | J1 | 2/3 | 3/3 | The review finds at least 7 of the 10 planted mistakes. |
| rabbitmq-review-hard | J2 | 0/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P2, P4, P5, P7). |
| rabbitmq-review-hard | J3 | 2/3 | 3/3 | The review finds at least 4 of the 6 planted mistakes marked 'subtle' (P1, P2, P3, P4, P6, P7). |
| rabbitmq-review-hard | J5 | 0/3 | 3/3 | For P1, the review says that on RabbitMQ 4.3 a basic.nack with requeue does not count toward the delivery-limit, so the message loops foreve |
| rabbitmq-review-hard | P1 | 0/3 | 3/3 | Version fact, existing code next to the change: every failure that is not a JsonException is nacked with requeue: true, and the comment trus |
| rabbitmq-review-hard | P2 | 2/3 | 3/3 | Over two files: the change adds x-dead-letter-strategy to the shared WorkQueueArguments(), which RabbitMqConsumer.cs line 55 also uses for t |
| rabbitmq-review-hard | P3 | 2/3 | 3/3 | Quiet default: dead-letter-strategy at-least-once needs overflow reject-publish. With the default overflow drop-head, the quorum queue falls |
| rabbitmq-review-hard | P8 | 2/3 | 3/3 | A payment for an unknown order is logged and acked: the money was taken and the result is gone. Finance needs every result. Send it to the d |
| redis-hard | C6 | 0/3 | 3/3 | HybridCache has no backplane and L1 lives 30 minutes: node B shows the ended promotion. |
| redis-hard | C7 | 1/3 | 3/3 | Checkout reads the price through the cache (existing code): node B charges the ended promotion. |
| redis-hard | C9 | 1/3 | 3/3 | The key has no version segment: an entry of the previous version gives listPrice 0. |
| redis-hard | J3 | 0/3 | 3/3 | The final message says that the app resolved StackExchange.Redis 2.7.27 through the cache package, and names the direct 3.x reference (or na |
| redis-review-hard | J1 | 2/3 | 3/3 | The review finds at least 8 of the 10 planted mistakes. |
| redis-review-hard | J5 | 0/3 | 3/3 | For P1, the review says that StackExchange.Redis 2.13.17 is the old 2.x line and recommends 3.x (3.4.5). It does not recommend keeping 2.x. |
| redis-review-hard | P1 | 0/3 | 3/3 | Version fact, over two files: the change adds a direct StackExchange.Redis 2.13.17 ('latest stable 2.x'). The current line is 3.x (3.4.5). T |
| redis-review-hard | P10 | 2/3 | 3/3 | MaximumPayloadBytes = 1024 * 1024 is the default (1 MB), so the line changes nothing and the comment is wrong: a larger product still is not |
| redis-review-hard | D5 | 2/3 | 3/3 | Decoy, must not be flagged: The direct Redis key orders:v1:lock:promotion-expiry carries the service prefix itself. That is correct: the raw |
| sqlserver-hard | J5 | 0/3 | 3/3 | The final message lists DATA-26 (RCSI) as an assumed decision or an open point, because the new report reads the Orders table while orders a |
| sqlserver-review-hard | J2 | 1/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P2, P7). |
| sqlserver-review-hard | J4 | 2/3 | 3/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| sqlserver-review-hard | J5 | 1/3 | 3/3 | For P4, the review says that the datetime2 column holds UTC and must be converted with AT TIME ZONE 'UTC' first (AT TIME ZONE 'UTC' AT TIME  |
| sqlserver-review-hard | P1 | 2/3 | 3/3 | Over two files, next to existing code: DoneAsync loads each StockItem tracked and calls the existing StockItem.Ship, and SaveChanges writes  |
| sqlserver-review-hard | P2 | 2/3 | 3/3 | Over two files: Program.cs turns on EnableRetryOnFailure. BeginTransactionAsync outside CreateExecutionStrategy().ExecuteAsync throws Invali |
| sqlserver-review-hard | P5 | 0/3 | 3/3 | Over two files: Zone is varchar(8) (PickingConfiguration). The interpolated {zone} in FromSql goes as nvarchar(4000), so under the default S |
| sqlserver-review-hard | P6 | 2/3 | 3/3 | Version fact: Guid.CreateVersion7() is not time-ordered in SQL Server. SQL Server sorts uniqueidentifier by its last 6 bytes first, and thos |
| sqlserver-review-hard | D1 | 2/3 | 3/3 | Decoy, must not be flagged: ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE with suppressTransaction: true is  |
| wolverine-hard | C5 | 2/3 | 3/3 | Wolverine 6 does not scan for IForwardsTo: version 1 events have no handler without RegisterMessageForwarder. |
| wolverine-review-hard | P3 | 2/3 | 3/3 | Over two files (docs/warehouse.md): the warehouse team declares and owns warehouse.pick-orders (quorum, x-dead-letter-exchange warehouse.dlx |
| wolverine-review-hard | P4 | 2/3 | 3/3 | The policy for the parcel platform retries for about 1.3 seconds (50 ms, 250 ms, 1 s) and then moves the message to the error queue. docs/ca |

## no-effect (246)

| Case | Item | Baseline | Skill | What it tests |
|------|------|----------|-------|---------------|
| aspnetcore-openapi-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| aspnetcore-openapi-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| aspnetcore-openapi-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| aspnetcore-openapi-hard | C4 | 3/3 | 3/3 | XML comments on private handlers do not reach the document. |
| aspnetcore-openapi-hard | C9 | 3/3 | 3/3 | The build-time document starts the app; the fix must skip the migration only for the document build. |
| aspnetcore-openapi-hard | C10 | 3/3 | 3/3 | The build-time document goes to obj/ by default. |
| aspnetcore-openapi-hard | C12 | 3/3 | 3/3 | Swashbuckle, the deprecated WithOpenApi, or the Microsoft.OpenApi 1.x namespaces. |
| aspnetcore-openapi-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| aspnetcore-openapi-review-hard | J2 | 3/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1 and P8). |
| aspnetcore-openapi-review-hard | J4 | 3/3 | 3/3 | At most 3 findings are false positives. A false positive is a claim that is wrong, or a fix that would break the code or that uses an API th |
| aspnetcore-openapi-review-hard | J5 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| aspnetcore-openapi-review-hard | J6 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict. |
| aspnetcore-openapi-review-hard | P1 | 3/3 | 3/3 | MigrateOnStartup is now false in the base settings, so Production no longer migrates the database. DatabaseOptions.cs (line 7) says that the |
| aspnetcore-openapi-review-hard | P2 | 3/3 | 3/3 | OpenApiDocumentsDirectory is $(MSBuildProjectDirectory)/../docs/openapi, which is src/docs/openapi, not docs/openapi at the root. The build  |
| aspnetcore-openapi-review-hard | P8 | 3/3 | 3/3 | A global JsonStringEnumConverter changes the existing wire format: ReturnResponse.Reason and Status change from numbers to names for every e |
| aspnetcore-openapi-review-hard | P9 | 3/3 | 3/3 | WithOpenApi is deprecated in .NET 10 (ASPDEPR002), and the csproj (line 8) hides the warning with NoWarn. The description does not reach the |
| aspnetcore-openapi-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: POST /orders has no ProducesValidationProblem. Correct: its union has ValidationProblem, so the document already |
| aspnetcore-openapi-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: using Microsoft.OpenApi; with JsonSchemaType.String, ParameterLocation.Header, and operation.Parameters ??= [].  |
| aspnetcore-openapi-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: MapOpenApi and MapScalarApiReference only in Development, so the partner cannot read the live document in Produc |
| aspnetcore-openapi-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: ExcludeFromDescription on the admin group also hides POST /admin/orders/{id}/ship, which ShippingEndpoints maps  |
| dotnet-observability-hard | C1 | 3/3 | 3/3 | CA1873 on a LogInformation call with arguments, also when CA1848 is off. |
| dotnet-observability-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| dotnet-observability-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| dotnet-observability-hard | C4 | 3/3 | 3/3 | probe_tests |
| dotnet-observability-hard | C5 | 3/3 | 3/3 | probe_tests |
| dotnet-observability-hard | C6 | 3/3 | 3/3 | The existing refusal log writes the gateway answer, which echoes the recipient. |
| dotnet-observability-hard | C7 | 3/3 | 3/3 | Span attributes are not redacted: the gateway answer or the address on a span. |
| dotnet-observability-hard | C8 | 3/3 | 3/3 | probe_tests |
| dotnet-observability-hard | C11 | 3/3 | 3/3 | IncludeScopes is false by default: the ShipmentId scope never reaches Application Insights. |
| dotnet-observability-hard | C12 | 3/3 | 3/3 | The Orders.Shipping source and meter are not registered with the distro. |
| dotnet-observability-hard | C13 | 3/3 | 3/3 | A meter created with new, a console exporter, an instrumentation key, a classic API, a second distro. |
| dotnet-observability-hard | C14 | 3/3 | 3/3 | No test proves the log of a refusal (reason, no personal data). |
| dotnet-observability-hard | C15 | 3/3 | 3/3 | No test proves the booking metric and its tags. |
| dotnet-observability-hard | J2 | 3/3 | 3/3 | The final message says that log scopes did not reach Application Insights before (IncludeScopes was off), so the ShipmentId filter of suppor |
| dotnet-observability-hard | J3 | 3/3 | 3/3 | The final message says that the existing Orders.Shipping source and meter were not registered, so the dispatch spans and the dashboard metri |
| dotnet-observability-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| dotnet-observability-review-hard | J3 | 3/3 | 3/3 | The review finds at least 3 of the 6 planted mistakes marked 'subtle' (P1, P2, P3, P4, P9, P10). |
| dotnet-observability-review-hard | J4 | 3/3 | 3/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| dotnet-observability-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| dotnet-observability-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| dotnet-observability-review-hard | P4 | 3/3 | 3/3 | Existing code that the change keeps in the new refusal path: each non-success answer is logged in full. The 422 answer echoes the recipient  |
| dotnet-observability-review-hard | P5 | 3/3 | 3/3 | Metric tag orders.postal_code with the full postal code: thousands of time series per carrier and outcome, and a postal code with the house  |
| dotnet-observability-review-hard | P6 | 3/3 | 3/3 | The meter is created with new Meter in a static field, not through IMeterFactory. Nobody disposes it, DI cannot scope it, and tests cannot c |
| dotnet-observability-review-hard | P9 | 3/3 | 3/3 | Over two files: the histogram declares the unit 's' (CarrierMetrics.cs line 14), but the gateway records elapsed.TotalMilliseconds. The dash |
| dotnet-observability-review-hard | P10 | 3/3 | 3/3 | Over two files: the test that must prove 'no recipient data in the log' stubs a 422 answer without the recipient echo. The real gateway echo |
| dotnet-observability-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: The shipment id as a span attribute is correct. IDs belong on spans and logs; only metric tags must be low-cardi |
| dotnet-observability-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: ConfigureOpenTelemetryTracerProvider and ConfigureOpenTelemetryMeterProvider before UseAzureMonitor, inside the  |
| dotnet-observability-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: TimeProvider.GetTimestamp and GetElapsedTime are correct for the duration (a monotonic clock, testable). There i |
| dotnet-observability-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: The catch filter is correct: it turns a connection failure and the HttpClient timeout (TaskCanceledException wit |
| dotnet-observability-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: A [LoggerMessage] method at Level Information with arguments is correct and builds clean: CA1873 warns on direct |
| elasticsearch-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| elasticsearch-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| elasticsearch-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| elasticsearch-hard | C5 | 3/3 | 3/3 | probe_tests |
| elasticsearch-hard | C6 | 3/3 | 3/3 | The descriptions use the standard analyzer: no stemming. |
| elasticsearch-hard | C8 | 3/3 | 3/3 | The stock change is indexed with the version of the product and dropped as a 409. |
| elasticsearch-hard | C12 | 3/3 | 3/3 | tests_added_min |
| elasticsearch-hard | J1 | 3/3 | 3/3 | The change fills the new index (or the changed mapping) with all existing products from the database (a backfill of the outbox, a rebuild jo |
| elasticsearch-hard | J2 | 3/3 | 3/3 | The external version of a product document grows with each change of the product and of its stock (for example Product.Version plus a long c |
| elasticsearch-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| elasticsearch-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| elasticsearch-review-hard | P2 | 3/3 | 3/3 | The alias moves to the new physical index products_v2, but nothing fills it: no rebuild from the database (backfill of the outbox, or a rein |
| elasticsearch-review-hard | P3 | 3/3 | 3/3 | Dual write: the descriptions endpoint writes to Elasticsearch in the request after SaveChangesAsync, with Refresh.WaitFor, and does not chec |
| elasticsearch-review-hard | P4 | 3/3 | 3/3 | Over two files: the external version adds StockItem.Version, which is the PostgreSQL xmin (IsRowVersion on a uint, CatalogConfiguration.cs l |
| elasticsearch-review-hard | P6 | 3/3 | 3/3 | The German description has no analyzer, so it uses the standard analyzer: 'Fahrrad' does not find 'Fahrräder' (no stemming, also not with fu |
| elasticsearch-review-hard | P8 | 3/3 | 3/3 | The stock clause is in must: it scores and is not cached. It does not change the score in a useful way. Put it in bool.filter with the activ |
| elasticsearch-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: page * PageSize > MaxResults is the correct cap: page 500 has from 9,980 + size 20 = 10,000, which index.max_res |
| elasticsearch-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: stock is { Available: > 0 } is correct although CatalogConfiguration ignores Available: Available is a C# proper |
| elasticsearch-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: ExistsAliasAsync(Name, a => a.Indices(PhysicalName)) sends HEAD /products_v2/_alias/products: it checks that the |
| elasticsearch-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: Fuzziness AUTO on a field with the dutch analyzer is correct: the query text is analyzed (stemmed) first, and th |
| fluentvalidation-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| fluentvalidation-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| fluentvalidation-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| fluentvalidation-hard | C4 | 3/3 | 3/3 | FindAsync(...) is not null accepts a deactivated product. |
| fluentvalidation-hard | C5 | 3/3 | 3/3 | The existing ValidationFilter returns ToDictionary() without the error codes. |
| fluentvalidation-hard | C6 | 3/3 | 3/3 | The duplicate check compares SKUs that the catalog normalizes (trim, upper case) with an ordinal compare. |
| fluentvalidation-hard | C7 | 3/3 | 3/3 | The built-in AddValidation filter answers first, the existing WithErrorCode covers only MaximumLength, or no cascade gives two errors. |
| fluentvalidation-hard | C11 | 3/3 | 3/3 | A sync Validate call stays (rule FV-SYNC-VALIDATE), or a removed or deprecated API. |
| fluentvalidation-hard | C12 | 3/3 | 3/3 | A mocked validator hides the sync call and the missing codes (rule FV-MOCK-VALIDATOR). |
| fluentvalidation-hard | J2 | 3/3 | 3/3 | The order line limit (MaxLinesPerOrder) is checked in one place: the second check in the POST /orders handler is removed, or the final messa |
| fluentvalidation-hard | J3 | 3/3 | 3/3 | No existing error code changes its meaning: a SKU longer than 32 characters still gives order.sku_invalid in the import report, or the final |
| fluentvalidation-hard | J5 | 3/3 | 3/3 | The final message marks the shape of the codes in the 400 response (decision API-20, not in decisions.md) as an assumed decision. |
| fluentvalidation-hard | J6 | 3/3 | 3/3 | An HTTP test asserts the 400 and the code of at least one new rule (a product that is not sold, or a product twice). |
| fluentvalidation-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| fluentvalidation-review-hard | J1 | 3/3 | 3/3 | The review finds at least 7 of the 9 planted mistakes. |
| fluentvalidation-review-hard | J2 | 3/3 | 3/3 | The review finds every planted mistake with severity 'high' (P1, P3, P7). |
| fluentvalidation-review-hard | J4 | 3/3 | 3/3 | At most 3 findings are false positives. A false positive is a claim that is wrong, a finding on a decoy, or a fix that would break the code  |
| fluentvalidation-review-hard | J5 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| fluentvalidation-review-hard | J6 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict. |
| fluentvalidation-review-hard | J7 | 3/3 | 3/3 | The fixes use FluentValidation 12 APIs: NotNull() before SetValidator, WithErrorCode after each validator, RuleLevelCascadeMode or .Cascade( |
| fluentvalidation-review-hard | P1 | 3/3 | 3/3 | RuleFor(x => x.Address).SetValidator(addressValidator) without NotNull(). SetValidator skips a null child, so "address": null passes the val |
| fluentvalidation-review-hard | P2 | 3/3 | 3/3 | WithErrorCode("customer.email_invalid") applies only to MaximumLength. An empty email gets the code NotEmptyValidator and an invalid email g |
| fluentvalidation-review-hard | P3 | 3/3 | 3/3 | The change removes the catch of the unique violation in CreateAsync and relies on the MustAsync email check in CreateCustomerRequestValidato |
| fluentvalidation-review-hard | P4 | 3/3 | 3/3 | UpdateCustomerRequest keeps its DataAnnotations (Required, EmailAddress, StringLength) while Program.cs calls AddValidation(). The built-in  |
| fluentvalidation-review-hard | P5 | 3/3 | 3/3 | The validator allows an address line of 200 characters, but CustomerConfiguration (line 15) and the migration make the column character vary |
| fluentvalidation-review-hard | P7 | 3/3 | 3/3 | The CountryCode rule has NotEmpty() and then Must(code => ... code.Trim() ...) without .Cascade(CascadeMode.Stop) (the other rules of this v |
| fluentvalidation-review-hard | P9 | 3/3 | 3/3 | Customer.Update stores the email with Trim() only, while the constructor (line 21) stores Trim().ToLowerInvariant(). The unique index is cas |
| fluentvalidation-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: .MaximumLength(100).WithMessage(...).WithErrorCode(...) is correct: WithMessage and WithErrorCode both apply to  |
| fluentvalidation-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: .Cascade(CascadeMode.Stop) on a single rule is a supported FluentValidation 12 API. Version 12 removed CascadeMo |
| fluentvalidation-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: new InlineValidator<UpdateCustomerRequest>() in the filter test is the recommended way to give a consumer a vali |
| mailkit-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| mailkit-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| mailkit-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| mailkit-hard | C7 | 3/3 | 3/3 | probe_tests |
| mailkit-hard | C9 | 3/3 | 3/3 | One message for all recipients: one rejected address stops the mail to everybody. |
| mailkit-hard | C10 | 3/3 | 3/3 | The delivery instructions go into the HTML body without encoding (copied from the return mail). |
| mailkit-hard | C12 | 3/3 | 3/3 | MimeKit 4.15.0 accepts CR LF in a quoted local-part (CVE-2026-30227). |
| mailkit-hard | C13 | 3/3 | 3/3 | No test against a real SMTP server, or Mailpit without a pinned tag. |
| mailkit-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| mailkit-review-hard | J4 | 3/3 | 3/3 | The review flags none of the decoys (D1 to D5), and has at most 2 other false positives. A false positive is a claim that is wrong, or a fix |
| mailkit-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| mailkit-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| mailkit-review-hard | P3 | 3/3 | 3/3 | One outbox row with the customer and all notification addresses in To (the sender parses the list with InternetAddressList.Parse, OutboxMail |
| mailkit-review-hard | P4 | 3/3 | 3/3 | Every SmtpCommandException is retried, also a 5xx (a rejected address or message). The row is sent again every 30 seconds for one day and th |
| mailkit-review-hard | P5 | 3/3 | 3/3 | SaveChangesAsync runs only when the whole batch succeeds. When the connection breaks in the middle of a batch (or the relay is down, or an e |
| mailkit-review-hard | P6 | 3/3 | 3/3 | Existing code that the change moves to the outbox: the return is saved first, and the outbox row in a second SaveChangesAsync (line 52). Whe |
| mailkit-review-hard | P8 | 3/3 | 3/3 | Each sent mail logs the recipient list and the full text body at Information level (personal data: addresses, delivery instructions, order l |
| mailkit-review-hard | P9 | 3/3 | 3/3 | Notification addresses are for business customers, but the endpoint does not check the customer kind: a consumer can also store 5 extra addr |
| mailkit-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: [MaxLength(5)] on IReadOnlyList<string> is correct: MaxLengthAttribute counts the items of a collection, and the |
| mailkit-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: HtmlEncoder.Default.Encode on the tracking URL inside href is correct: an attribute value must have & encoded as |
| mailkit-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: PrimitiveCollection with the backing field _notificationAddresses and field access maps the list to a PostgreSQL |
| mailkit-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: The Message-ID is made from the outbox row ID and the own sender domain. It is stable, so a mail that is sent ag |
| mailkit-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: The retry delay is 5 s, 10 s, 20 s, then 30 s (Attempts is incremented first, so the first delay is 5 * 2^0). Th |
| postgresql-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| postgresql-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| postgresql-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| postgresql-hard | C4 | 3/3 | 3/3 | Snapshot matches the model (has-pending-model-changes). |
| postgresql-hard | C5 | 3/3 | 3/3 | probe_tests |
| postgresql-hard | C10 | 3/3 | 3/3 | ADD COLUMN with a volatile default rewrites the orders table under an ACCESS EXCLUSIVE lock. |
| postgresql-hard | C12 | 3/3 | 3/3 | Clock outside TimeProvider, raw SQL built from strings, a stricter isolation level without a retry, or a guessable tracking code from Random |
| postgresql-hard | J5 | 3/3 | 3/3 | The tracking code comes from a cryptographic random source (RandomNumberGenerator, gen_random_uuid, or gen_random_bytes), not from Random, t |
| postgresql-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| postgresql-review-hard | J1 | 3/3 | 3/3 | The review finds at least 7 of the 9 planted mistakes. |
| postgresql-review-hard | J3 | 3/3 | 3/3 | The review finds at least 4 of the 6 planted mistakes marked 'subtle' (P2, P3, P4, P5, P6, P9). |
| postgresql-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| postgresql-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| postgresql-review-hard | P1 | 3/3 | 3/3 | SQL injection: the query string 'sort' (line 79) goes into the SQL text as an identifier (ORDER BY "{orderBy}") and runs through SqlQueryRaw |
| postgresql-review-hard | P3 | 3/3 | 3/3 | date_trunc('month', o."PlacedAt") on timestamptz uses the session TimeZone (UTC on the server), and the WHERE clause uses UTC midnight (line |
| postgresql-review-hard | P4 | 3/3 | 3/3 | DDL on a live table: the new check constraint makes the migration run ALTER TABLE order_lines ADD CONSTRAINT ... CHECK (migration line 67) w |
| postgresql-review-hard | P5 | 3/3 | 3/3 | Race: the old stock level is read in one statement and the upsert runs in another (line 43 to 47), without a transaction or a row lock. With |
| postgresql-review-hard | P7 | 3/3 | 3/3 | NOT IN (SELECT o."CouponCode" FROM orders) on a nullable column: most orders have no coupon, so the subquery returns NULL and NOT IN returns |
| postgresql-review-hard | P8 | 3/3 | 3/3 | Instant stored as a wall-clock time: NotifiedAt is a DateTime in a 'timestamp without time zone' column (ProductConfiguration.cs line 29), f |
| postgresql-review-hard | P9 | 3/3 | 3/3 | Over two files: a SERIALIZABLE transaction without a retry. Two orders at the same time that read the same rows fail with 40001 (serializati |
| postgresql-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: LIKE on a column with a nondeterministic ICU collation works on PostgreSQL 18 (checked on postgres:18.6: 'Café C |
| postgresql-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: ADD COLUMN "Express" boolean NOT NULL DEFAULT false on the big orders table does not rewrite the table: since Po |
| puppeteersharp-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| puppeteersharp-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| puppeteersharp-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| puppeteersharp-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| puppeteersharp-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| puppeteersharp-review-hard | P1 | 3/3 | 3/3 | The purchase order number (CustomerReference, free text from the webshop) goes into the HTML without HtmlEncoder, while every other value of |
| puppeteersharp-review-hard | P2 | 3/3 | 3/3 | Over two files: the fetcher checks the addresses of the host with Dns.GetHostAddressesAsync (line 18), and then HttpClient connects with its |
| puppeteersharp-review-hard | P6 | 3/3 | 3/3 | The browser is a Lazy<Task<IBrowser>>. When Chrome crashes or disconnects (IsConnected false), or the first launch fails, the Lazy keeps the |
| puppeteersharp-review-hard | P7 | 3/3 | 3/3 | The request interception allows a URL when it starts with an origin string (DocumentOptions.cs line 19): 'https://cdn.example.com' also matc |
| puppeteersharp-review-hard | P8 | 3/3 | 3/3 | cap_add SYS_ADMIN for the Chrome sandbox gives the container a far wider capability than the sandbox needs (mount, namespaces, and more). Do |
| puppeteersharp-review-hard | P9 | 3/3 | 3/3 | ReadAsByteArrayAsync reads the whole logo with no size limit (HttpClient buffers up to 2 GB by default). A customer URL that serves a very l |
| puppeteersharp-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: The customer logo data URI goes into src without HtmlEncoder. It is safe: the value is 'data:' plus a media type |
| puppeteersharp-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: ExecutablePath = _options.ChromePath, which is null by default, is correct: with a null ExecutablePath Puppeteer |
| rabbitmq-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| rabbitmq-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| rabbitmq-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| rabbitmq-hard | C5 | 3/3 | 3/3 | The default JsonSerializerOptions of the base class do not read the camelCase body of the gateway. |
| rabbitmq-hard | C6 | 3/3 | 3/3 | probe_tests |
| rabbitmq-hard | C7 | 3/3 | 3/3 | The same paymentId comes again with a new message_id. |
| rabbitmq-hard | C11 | 3/3 | 3/3 | Existing consumer: a body that cannot be read stays unacked. |
| rabbitmq-hard | C13 | 3/3 | 3/3 | Changed arguments of an existing queue fail the redeclaration on the production broker. |
| rabbitmq-hard | J2 | 3/3 | 3/3 | order.paid.v1 is written as an outbox row in the same SaveChangesAsync as the stored payment and the order status, and the consumer acks the |
| rabbitmq-hard | J5 | 3/3 | 3/3 | A payment.failed.v1 result stores a payment with the status Failed and its reason, and leaves the order Placed. A test covers it. |
| rabbitmq-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| rabbitmq-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| rabbitmq-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| rabbitmq-review-hard | P4 | 3/3 | 3/3 | Over two files: the consumer deduplicates on message_id, but docs/payment-gateway-events.md (line 28) says that the gateway publishes the sa |
| rabbitmq-review-hard | P5 | 3/3 | 3/3 | order.paid.v1 is published after SaveChangesAsync, not through the outbox (decision MSG-09). When the broker is down or the process stops af |
| rabbitmq-review-hard | P6 | 3/3 | 3/3 | Version fact (RabbitMQ.Client 7): confirms are on, but publisherConfirmationTrackingEnabled is false. Then BasicPublishAsync does not wait f |
| rabbitmq-review-hard | P7 | 3/3 | 3/3 | Over two files: the base class deserializes with the default JsonSerializerOptions (case-sensitive). The warehouse sends PascalCase, but the |
| rabbitmq-review-hard | P9 | 3/3 | 3/3 | A failed payment cancels the order, and TimeSpan.MaxValue bypasses the cancel window. The order must stay Placed so that the customer can pa |
| rabbitmq-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: ea.Body.ToArray() before the first await is correct: it copies the body while the memory is still valid. The cli |
| rabbitmq-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: BasicNackAsync with requeue: false for a JsonException is correct: a body that cannot be read never succeeds, an |
| rabbitmq-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: MarkPaid returns without an error when the order is already Paid. That is the idempotent state check of principl |
| rabbitmq-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: The one channel of the singleton is guarded by a SemaphoreSlim, so two publishes never use it at the same time.  |
| redis-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| redis-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| redis-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| redis-hard | C5 | 3/3 | 3/3 | probe_tests |
| redis-hard | C8 | 3/3 | 3/3 | The cached price outlives endsAt. |
| redis-hard | C10 | 3/3 | 3/3 | Invented cache APIs, sync over async, KEYS, a memory cache as the shared L2, and a blocking connect. |
| redis-hard | J1 | 3/3 | 3/3 | Every write path that changes a product (the new promotion endpoints, and the existing price change) saves to the database first and then re |
| redis-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| redis-review-hard | J3 | 3/3 | 3/3 | The review finds at least 3 of the 5 planted mistakes marked 'subtle' (P1, P3, P5, P6, P8). |
| redis-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| redis-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| redis-review-hard | P2 | 3/3 | 3/3 | The new promotion endpoints remove the cache key before SaveChangesAsync (StartPromotionAsync lines 95-96, EndPromotionAsync lines 114-115), |
| redis-review-hard | P4 | 3/3 | 3/3 | The product read uses the default entry options: L1 for 30 minutes (Program.cs line 53, equal to L2). RemoveAsync on one replica does not cl |
| redis-review-hard | P5 | 3/3 | 3/3 | The cached value is the ProductResponse with the price computed at caching time (ProductContracts.cs line 35). The entry lives 30 minutes, s |
| redis-review-hard | P7 | 3/3 | 3/3 | ConnectionMultiplexer.ConnectAsync with the plain connection string keeps abortConnect=true (the default): when Valkey is down at startup, t |
| redis-review-hard | P8 | 3/3 | 3/3 | Over two files: the job removes $"orders:product:{sku}" through HybridCache, but AddStackExchangeRedisCache already adds InstanceName "order |
| redis-review-hard | P9 | 3/3 | 3/3 | PingAsync().Result blocks a thread pool thread in each readiness probe (sync over async, SER307 in StackExchange.Redis 3.x). Under load it s |
| redis-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: LockReleaseAsync(LockKey, token) in finally is correct: it deletes the lock only when the token is still ours. I |
| redis-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: A new DI scope for each run in the singleton BackgroundService is the correct way to use the scoped OrdersDbCont |
| redis-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: Math.Round to 2 decimals with MidpointRounding.AwayFromZero is correct and matches the VAT rounding of PriceCalc |
| sqlserver-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| sqlserver-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| sqlserver-hard | C4 | 3/3 | 3/3 | Snapshot matches the model (has-pending-model-changes). |
| sqlserver-hard | C12 | 3/3 | 3/3 | UUIDv7 or NEWID() for a new clustered key: random order in SQL Server. |
| sqlserver-hard | J1 | 3/3 | 3/3 | The existing sales report counts an order on the correct Amsterdam day (the datetime2 value is converted from UTC first, and the range uses  |
| sqlserver-hard | J3 | 3/3 | 3/3 | The voucher use is one conditional UPDATE (or ExecuteUpdateAsync with a condition on the uses) with a check of the row count, in the same tr |
| sqlserver-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| sqlserver-review-hard | J1 | 3/3 | 3/3 | The review finds at least 7 of the 10 planted mistakes. |
| sqlserver-review-hard | J3 | 3/3 | 3/3 | The review finds at least 3 of the 6 planted mistakes marked 'subtle' (P1, P2, P4, P5, P6, P10). |
| sqlserver-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| sqlserver-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| sqlserver-review-hard | P3 | 3/3 | 3/3 | MERGE dbo.PickZones without HOLDLOCK. When the warehouse system sends the zone of a new SKU twice at the same time, both sessions see no row |
| sqlserver-review-hard | P4 | 3/3 | 3/3 | Over two files: PickingConfiguration maps PickedAt to datetime2 in UTC (value converter). AT TIME ZONE reads a datetime2 without offset as a |
| sqlserver-review-hard | P7 | 3/3 | 3/3 | The sort query parameter goes into the SQL text (ORDER BY {orderBy}) through FromSqlRaw: SQL injection. The local variable also hides the EF |
| sqlserver-review-hard | P8 | 3/3 | 3/3 | WITH (NOLOCK): dirty reads, and a scan can read a row twice or miss it while pages split. The same change turns on RCSI (DATA-26), so the re |
| sqlserver-review-hard | P9 | 3/3 | 3/3 | Check-then-insert for the pick task of an order: AnyAsync (line 41) and then SaveChanges. Two parallel calls for one order both pass the che |
| sqlserver-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: The queue claim is correct: one UPDATE of a CTE with TOP (1) ... ORDER BY CreatedAt, WITH (UPDLOCK, READPAST, RO |
| sqlserver-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: MERGE dbo.StockItems WITH (HOLDLOCK) from OPENJSON is correct. The JSON parameter is nvarchar(max), which OPENJS |
| sqlserver-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam") in C# is correct: .NET 6 and later accept IANA ids on Li |
| sqlserver-review-hard | D5 | 3/3 | 3/3 | Decoy, must not be flagged: The index (Zone, Status, CreatedAt) serves the claim and the open list: the two equality columns first, then the |
| wolverine-hard | C1 | 3/3 | 3/3 | build_warnaserror (generic) |
| wolverine-hard | C2 | 3/3 | 3/3 | tests_pass (generic) |
| wolverine-hard | C3 | 3/3 | 3/3 | package_policy (generic) |
| wolverine-hard | C4 | 3/3 | 3/3 | A handler that injects the typed HttpClient ICarrierClient throws InvalidServiceLocationException in Wolverine 6. |
| wolverine-hard | C6 | 3/3 | 3/3 | The inbox removes duplicates only by envelope id; MarkPaid ignores a second capture, so the booking runs twice. |
| wolverine-hard | C7 | 3/3 | 3/3 | The existing catch-and-log and the 1.3 s catch-all retry lose the booking during a short platform outage. |
| wolverine-hard | C8 | 3/3 | 3/3 | A thrown address refusal is retried by the catch-all policy and never sets AddressProblem. |
| wolverine-hard | C9 | 3/3 | 3/3 | probe_tests |
| wolverine-hard | C10 | 3/3 | 3/3 | probe_tests |
| wolverine-hard | C11 | 3/3 | 3/3 | Production uses TypeLoadMode.Static: a new handler without codegen write throws MissingPreBuiltTypesException at the start. |
| wolverine-hard | C13 | 3/3 | 3/3 | The host is made to start by turning Wolverine 6 checks off, or tests wait with fixed delays. |
| wolverine-hard | J1 | 3/3 | 3/3 | The existing POST /shipments/{id}/book stores the booking and its outgoing messages in one transaction (IDbContextOutbox<T> with SaveChanges |
| wolverine-hard | J2 | 3/3 | 3/3 | The catch-and-log in the existing PaymentCapturedHandler is gone (exceptions reach the error policies), or the final message reports it as a |
| wolverine-hard | J3 | 3/3 | 3/3 | The booking sends a reference to the parcel platform that stays the same for each retry of the same parcel (for example the shipment id that |
| wolverine-review-hard | C1 | 3/3 | 3/3 | files_unchanged (generic) |
| wolverine-review-hard | J3 | 3/3 | 3/3 | The review finds at least 5 of the 8 planted mistakes marked 'subtle' (P1, P2, P3, P5, P6, P7, P8, P9). |
| wolverine-review-hard | J6 | 3/3 | 3/3 | Each finding gives the file and the line (or quotes the code) and a concrete fix. |
| wolverine-review-hard | J7 | 3/3 | 3/3 | The review separates blocking issues from minor ones and ends with a clear merge verdict that does not approve the change as it is. |
| wolverine-review-hard | P5 | 3/3 | 3/3 | The reference (the idempotency key of the platform) changes with each attempt (envelope.Attempts). When the platform books the parcel but th |
| wolverine-review-hard | P6 | 3/3 | 3/3 | Existing code next to the change: the manual booking saves with SaveChangesAsync and then calls bus.PublishAsync (line 106) and the new bus. |
| wolverine-review-hard | P7 | 3/3 | 3/3 | Existing code that the change keeps: the catch of every exception logs and returns an empty result. Wolverine then sees a success and acknow |
| wolverine-review-hard | P8 | 3/3 | 3/3 | Configuration fact, over two places: production uses TypeLoadMode.Static with AssertAllPreGeneratedTypesExist = true (MSG-13), but the chang |
| wolverine-review-hard | D1 | 3/3 | 3/3 | Decoy, must not be flagged: db.Shipments.Add without SaveChangesAsync, and BookParcel with shipment.Id (line 45), is correct: EF Core sets t |
| wolverine-review-hard | D2 | 3/3 | 3/3 | Decoy, must not be flagged: The handler changes the shipment and never calls SaveChangesAsync. That is correct: AutoApplyTransactions with t |
| wolverine-review-hard | D3 | 3/3 | 3/3 | Decoy, must not be flagged: UseDurableLocalQueues() is correct and needed: BookParcel goes through a local queue, and only a durable local q |
| wolverine-review-hard | D4 | 3/3 | 3/3 | Decoy, must not be flagged: Catching CarrierAddressRejectedException and marking the shipment AddressProblem is correct: a 422 is a business |
