# Team decisions

Source of truth for defaults in new code. Existing code keeps its style.

| ID | Question | Answer | Status | By | ADR | Check | Date |
|----|----------|--------|--------|----|-----|-------|------|
| API-01 | HTTP endpoints: minimal APIs or controllers? | Minimal APIs, grouped with MapGroup, one static endpoint class per feature | accepted | human | - | judge | 2026-09-02 |
| DATA-01 | Database engine? | SQL Server 2025 (compatibility level 170). The customer contract requires SQL Server. | accepted | human | - | judge | 2026-09-02 |
| DATA-02 | Data access library? | EF Core only (no Dapper, no raw ADO.NET) | accepted | human | - | judge | 2026-09-02 |
| DATA-26 | RCSI on SQL Server? | On (READ_COMMITTED_SNAPSHOT), in each environment and in the test database. Turned on by a migration with suppressTransaction. | accepted | human | - | judge | 2026-10-19 |
| DATA-07 | Primary key type? | Guid. Keys must be time-ordered, so that new rows go to the end of the clustered index. No Guid.NewGuid() in code. | accepted | human | - | judge | 2026-09-02 |
| TEST-02 | Assertion library? | FluentAssertions 7.2.2 (pinned, Apache-2.0). Do not upgrade to 8. | accepted | human | - | Directory.Packages.props:FluentAssertions | 2026-09-02 |
| MSG-01 | Messages between parts of the service: in process or a broker? | In process. No message broker. | accepted | human | - | judge | 2026-09-02 |

A decision that is not in this table has no answer yet. Ask the human before new code depends on it.
