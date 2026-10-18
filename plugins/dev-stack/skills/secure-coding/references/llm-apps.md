# Code that calls an LLM

Read this file only when the code calls a model (chat, completion, embeddings, agent with tools). The IDs are from the OWASP Top 10 for LLM Applications 2026 (published 2026-08-03). It replaces the 2025 edition. The 2025 ID is in brackets.

## Rules

| ID | Risk | Rule for the code |
|----|------|-------------------|
| LLM01 (2025: LLM01) | Prompt injection | Keep user text and document text out of the system prompt. Pass it as a separate user message, marked as data. Do not let model output choose a tool and its arguments without a check. |
| LLM02 (LLM02) | Sensitive information disclosure | Put only the data that the user may see in the context. Filter the retrieval by tenant and by the rights of the user. Do not log full prompts with personal data. |
| LLM03 (LLM06) | Excessive agency | Give tools the rights of the user, not of the app (no app database connection with write rights). A destructive or costly tool needs a human confirmation. Give the fewest tools that the task needs. |
| LLM04 (LLM03) | Supply chain | Pin the model version and the SDK version. Check AI packages and MCP servers like each package (`supply-chain.md`). |
| LLM05 (LLM04) | Data and model poisoning | Do not index content that users can edit into the retrieval store without a review or a trust label. |
| LLM06 (LLM10) | Unbounded consumption | Set `MaxOutputTokens`, a timeout, a rate limit per user (SEC-11) and a budget alert. |
| LLM07 (LLM09) | Misinformation | Validate model output that changes a legal or financial value (HS code, customs value, a declaration) against rules or a human check. |
| LLM08 (LLM07) | Hidden context exposure | Never put secrets, connection strings or internal URLs in a prompt. Assume that the user can read the full prompt. |
| LLM09 (LLM08) | Vector and embedding weaknesses | Add the tenant filter to each vector search (Elasticsearch `knn` with a `filter`). Store the tenant with each chunk. |
| LLM10 (LLM05) | Improper output handling | Treat model output as untrusted input. Encode it for HTML (never `bypassSecurityTrustHtml`). Never run it as SQL or a command. Validate it before you use it as a path or a URL. |

## Agents

When the model acts with tools or memory, also read the OWASP Top 10 for Agentic Applications 2026 (ASI01 to ASI10). The main code rules: a least-privilege tool identity, and a human in the loop for actions that you cannot undo. Log each tool call with its arguments.

## Check

`judge`: LL-1 to LL-6 in `review-checklist.md`.
