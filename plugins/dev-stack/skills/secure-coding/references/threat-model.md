# Threat model light

Decision: SEC-18. Use this for each design that adds a trust boundary. Examples: a new external input, a new integration, a change to authentication, a new data class. Write the answers in the design or the ADR. ASVS 5.0 asks for documented security decisions.

## Frame

The four questions of the Threat Modeling Manifesto:

1. What are we working on?
2. What can go wrong?
3. What are we going to do about it?
4. Did we do a good enough job?

## Steps

1. Draw or list the data flow. Mark each trust boundary, for example browser to API, API to a store, API to an external service.
2. Mark each place where untrusted data enters: route, query, body, header, cookie, message, file, partner response, model output.
3. Answer the five questions for the design.
4. For each threat, name the control (a rule of SKILL.md or a SEC decision). A threat without a control is a finding. Done when each threat has a control or an accepted risk with an owner.

## The five questions

| # | STRIDE | Question | Typical controls |
|---|--------|----------|------------------|
| 1 | Spoofing | Who or what calls each entry point (user, service, message, webhook, LLM tool call), and how do we prove that identity? | JWT validation, client credentials, webhook HMAC, one RabbitMQ user per service |
| 2 | Tampering | Which values can the caller change (ids, prices, status, message bodies, file names, URLs), and where does the server check them again? | DTOs, server-owned state, resource checks, signature on messages |
| 3 | Repudiation | A user denies an action (a declaration, a payment, a deletion). Which record proves who did what and when? Can the user change that record? | Audit table, security events with `EventId` (SEC-23) |
| 4 | Information disclosure | Which data leaves this boundary (responses, logs, errors, cache, search index, e-mail, PDF, LLM prompt), and can another tenant ever see it? | Tenant filter, response DTOs, redaction, ProblemDetails |
| 5 | Denial of service and elevation of privilege | What is the most costly request that a user can send, and what limits it? What is the most that a compromised part (Pod, queue consumer, LLM tool) can do? | Rate limits, size limits, page caps, Pod security, least-privilege identities |

## Output template

```markdown
## Threat model

Boundaries: <list>
Untrusted inputs: <list>

| # | Threat | Control | Status |
|---|--------|---------|--------|
| S | <threat> | <control, rule or SEC ID> | done / planned / accepted risk (owner) |
| T | ... | ... | ... |
| R | ... | ... | ... |
| I | ... | ... | ... |
| D/E | ... | ... | ... |
```
