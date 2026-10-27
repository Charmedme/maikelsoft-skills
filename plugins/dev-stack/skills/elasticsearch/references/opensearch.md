# OpenSearch

Read this file only when the cluster is OpenSearch (for example AWS OpenSearch Service). The rules of `SKILL.md` apply in idea. The client and some APIs differ.

## Contents

- When OpenSearch
- Client
- What differs

## When OpenSearch

Decision SEARCH-04 chooses Elasticsearch 9.5. Choose OpenSearch when:

- The license of Elasticsearch (ELv2, AGPLv3, or SSPL) is not allowed.
- The platform is AWS and the team uses the managed OpenSearch Service.

## Client

| Item | Version on 2026-10-27 |
|------|-----------------------|
| OpenSearch.Client (high level) and OpenSearch.Net (low level) | 2.2.0 |
| OpenSearch server | 3.9.0, and 2.19.7 (2026-10-20) for the 2.x line |

`Elastic.Clients.Elasticsearch` does not work with OpenSearch: it checks the `X-Elastic-Product` header. OpenSearch.Client is a fork of NEST 7, so its API looks like NEST (`OpenSearchClient`, `ConnectionSettings`, `IsValid`), not like client 9.

## What differs

- The ideas stay the same: explicit strict mappings, aliases with a version suffix, external versions, and `bool.filter`. Also the 10,000 window, point in time with `search_after` (OpenSearch 2.4 and later), no `query_string` on raw input, and TLS validation on.
- Authentication on AWS uses SigV4 request signing (the package `OpenSearch.Net.Auth.AwsSigV4`), not an API key.
- The stackcheck rules for NEST (ES-USING-NEST, ES-PKG-NEST) do not apply to `OpenSearch.Client`.
- Check each API in the OpenSearch documentation. Do not take an Elasticsearch 9 example as proof.
