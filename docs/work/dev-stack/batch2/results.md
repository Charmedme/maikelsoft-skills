# Results: dev-stack batch 2 test round

Date: 2026-10-20. 132 runs: 11 skills x 2 cases (trap and review) x 2 variants (with and without the skill) x 3 runs. Model Opus, headless (`evals/dev-stack/run.py`).

The runs cannot use the Agent and Skill tools, so the account skills of the machine reach neither variant. Blind graders on Sonnet (`grade.py`). Checks and hidden probe tests with `score.py`. Total cost $160 (baseline $76, skill $84).

## Verdict

The skills help. Per case, the skill variant wins 13 cases, ties 8, and loses 1 (sign test p = 0.002).

- Trap cases: the skill variant passes 98% of the checks and judges, the baseline 85%. The baseline fails 29 checks over its 33 trap runs, the skill variant 1.
- Review cases: the skill variant finds 8.1 planted mistakes on average, the baseline 7.5. The skill variant has no false positives, the baseline 0.15 per review.
- Cost and time stay about the same: the skill variant costs about 10% more.

## Per case

| Case | Baseline (checks, judges) | Skill (checks, judges) | What the skill adds |
|------|---------------------------|------------------------|---------------------|
| mailkit trap | 7.7/12, 3.3/4 | 11.7/12, 4.0/4 | Outbox while the relay is down, no `Auto` TLS, the Pkcs pin, encoding |
| puppeteersharp trap | 7.0/8, 2.0/4 | 8.0/8, 4.0/4 | No `--no-sandbox` (the baseline adds it in 3 of 3 runs) |
| wolverine trap | 8.0/10, 1.7/2 | 10.0/10, 2.0/2 | Quorum queue (the baseline keeps the classic default in 3 of 3) |
| dotnet-observability trap | 6.0/7, 3.0/4 | 7.0/7, 4.0/4 | The sampling setting, the redaction answer |
| redis trap | 8.7/9, 0.7/3 | 9.0/9, 2.0/3 | No stale L1 on other nodes, the key and TTL rules |
| sqlserver trap | 6.0/6, 2.3/4 | 6.0/6, 3.7/4 | RCSI instead of `NOLOCK`, a stated deadlock retry |
| elasticsearch trap | 9.7/10, 3.7/5 | 10.0/10, 5.0/5 | PIT export, external versions, item-by-item bulk |
| openapi trap | 10.0/10, 2.0/3 | 10.0/10, 3.0/3 | States API-10 and API-11 as decisions |
| fluentvalidation trap | 7.7/8, 2.7/3 | 8.0/8, 3.0/3 | The null body in Production |
| rabbitmq trap | 7.7/8, 1.0/2 | 8.0/8, 1.7/2 | No fixed delays in tests, confirms |
| postgresql trap | 6.0/6, 4.0/4 | 6.0/6, 4.0/4 | Tie: the baseline already solves the picking queue and the time zone |
| reviews (11) | 7.5 planted found, 0.15 false positives | 8.1 planted found, 0 false positives | Wins on wolverine, elasticsearch, puppeteersharp. One judge loss on redis (J6 in 1 run) |

## Faults found and fixed during the round

- Disk full two times (Testcontainers volumes, images built from a Dockerfile). `run.py` now removes build output, unused volumes and built images after each run. The runs and scores that the full disk broke were run or scored again.
- Observability probe: a solution that reads the HMAC key from configuration (the safe choice) failed, because the probe host does not read the test settings. The probe now gives the redactor a test key. All 6 runs scored again.
- RabbitMQ probe: a solution that declares the topology just before its first publish failed, because the probe waited for the queue at start. The probe now places an order when the queue is missing. All 6 runs scored again, selftest 13/13.
- SQL Server containers do not start when memory is low (8 GB machine). Those scores were run again with free memory.

## Limits

- One model (Opus) for the runs. The value can differ for Sonnet.
- The review cases are near the ceiling: the baseline already finds 7 to 9 of 8 or 9 planted mistakes.
- Some judge assertions name the knowledge of the skill (for example J6 of redis-review). They measure whether that knowledge reaches the answer, and they favor the skill by design.
- 3 runs per variant. The per-case differences are small. The sign test over 22 cases is the main result.
