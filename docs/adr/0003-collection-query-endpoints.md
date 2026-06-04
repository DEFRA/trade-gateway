# ADR-0003: Collection Query Endpoints

**Status:** Proposed  
**Date:** 2026-05-28

---

## Context

Each document type in the Trade Gateway API (Intras, CHEDs, DOCOMs) needs an endpoint that returns a paginated list of documents updated within a given time window. Consumers use this to poll for changes incrementally rather than fetching individual documents by ID.

This does not fit the standard REST member-resource pattern (`GET /intras/{id}`) because the request is a query against the collection rather than a retrieval of a known member. Two structural options were considered:

1. **Query parameters on the collection URL** — `GET /intras?updatedFrom=...&updatedBefore=...`
2. **A dedicated operation URL** — `GET /intras/_updates?after=...&before=...`

Pagination strategy is constrained by the upstream TracesNT SOAP API, which uses offset-based pagination (`pageSize` + `offset`, both required, `pageSize` capped at 200) and returns no total item count. The stop condition is receiving fewer items than `pageSize`.

---

## Decision

### URL structure

Each resource type exposes its own collection URL with temporal filter parameters:

```
GET /intras?updatedFrom=2026-01-01T00:00:00Z&updatedBefore=2026-01-02T00:00:00Z&page=1&pageSize=20
GET /cheds?updatedFrom=...&updatedBefore=...&page=1&pageSize=20
GET /docoms?updatedFrom=...&updatedBefore=...&page=1&pageSize=20
```

This is the standard REST pattern for filtering a collection: query parameters narrow the result set without introducing a non-standard URL segment. The `updatedFrom`/`updatedBefore` names make the intent explicit without requiring a special operation segment.

Separate per-resource endpoints are used rather than a single unified `/changes?resourceType=...` endpoint because each resource type is handled by its own upstream service. A unified endpoint would require cross-service orchestration for no consumer benefit — consumers polling for updates already know which resource type they care about.

### Query parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `updatedFrom` | Yes | Return documents updated at or after this instant (inclusive). ISO 8601 UTC. |
| `updatedBefore` | Yes | Return documents updated strictly before this instant (exclusive). ISO 8601 UTC. |
| `page` | No | 1-based page number. Defaults to 1. |
| `pageSize` | No | Items per page. Defaults to 20, capped at 200 (the upstream maximum). |

Both `updatedFrom` and `updatedBefore` are required on every request. Open-ended time windows are not supported — they would produce unbounded result sets.

The semantics were verified against the TracesNT acceptance environment: the upstream applies a **half-open interval [From, To)** — `From` is inclusive, `To` is exclusive. A document with `UpdateDateTime = T` is returned when `From ≤ T` but not when `To = T`. The gateway parameter names reflect this directly: `updatedFrom` (inclusive) and `updatedBefore` (exclusive). Consumers can chain successive windows without overlap or gap by setting the next `updatedFrom` to the previous `updatedBefore`.

### Pagination

The upstream TracesNT `findEuIntraCertificate` and `findChedCertificate` operations use **offset-based pagination**: both `pageSize` and `offset` are required parameters (cardinality 1..1). `offset` is the number of items to skip; to retrieve page _N_, the gateway computes `offset = pageSize × (N − 1)`.

The gateway exposes a 1-based `page` number to consumers and translates it to an upstream offset internally. This avoids leaking the SOAP model while still mapping without any buffering or re-counting in the middle tier.

The upstream does not return a total item count. The termination condition is: stop requesting further pages when the number of items returned is less than `pageSize`. The gateway reflects this in the response envelope.

The response envelope:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "hasMore": true
}
```

`hasMore` is `true` when `items.length == pageSize` (there may be more pages), `false` when `items.length < pageSize` (this is the last page). Consumers must also treat an empty `items` array as a terminal condition: if the total result count is an exact multiple of `pageSize`, the final page will be full and `hasMore` will be `true`, but the subsequent request will return zero items. This edge case is inherited from the upstream API, which uses the same heuristic.

### Response content type

Each collection endpoint has its own vendor media type covering the full response — the pagination envelope and the summary items within it:

| Endpoint | Media type |
|----------|-----------|
| `GET /intras?...` | `application/vnd.defra.trade.intra-list.v1+json` |
| `GET /cheds?...` | `application/vnd.defra.trade.ched-list.v1+json` |

The items in the collection are **summary representations** of the resource, not the full detail returned by the single-resource endpoint. A summary contains enough fields to identify and filter records; consumers that need full detail should follow up with `GET /intras/{id}` or `GET /cheds/{id}`.

Content negotiation (per [ADR-0001](./0001-api-versioning-via-content-negotiation.md)) applies in the same way: pin a version via the `Accept` header, or omit it to receive the latest.

### Error responses

All errors follow [ADR-0002](./0002-rest-api-conventions.md) — RFC 7807 Problem Details with `application/problem+json`:

| Code | When used |
|------|-----------|
| `400 Bad Request` | `updatedFrom` or `updatedBefore` is missing, unparseable, or `updatedFrom` ≥ `updatedBefore`. |
| `403 Forbidden` | Caller is not permitted to query this resource type. |
| `502 Bad Gateway` | Upstream service communication failure. |

---

## Consequences

**Positive**

- Standard REST pattern — filtering a collection via query parameters needs no explanation in API documentation.
- Offset-based pagination aligns directly with the TracesNT SOAP API — the gateway translates a 1-based page number to an offset, which is a trivial calculation with no buffering or re-counting.
- Per-resource endpoints align with the per-service ownership model and keep routing, authorisation, and OpenAPI schemas cleanly separated.
- `updatedFrom`/`updatedBefore` names are self-describing without a special URL segment.

**Negative / Trade-offs**

- `updatedFrom` / `updatedBefore` present on the collection URL blurs the distinction between "list all" and "poll for changes"; a consumer skimming the OpenAPI schema may not realise they are required parameters for the polling use case.
- Offset-based pagination can produce inconsistent results if the underlying dataset changes between page requests (items added or removed can shift offsets). This is acceptable for the polling use case where eventual consistency across a crawl is tolerable.
- The upstream provides no total count. Consumers must use `hasMore` to detect the last page rather than calculating progress from a total.
- Both `updatedFrom` and `updatedBefore` are required — consumers cannot request an open-ended "everything since" query.
