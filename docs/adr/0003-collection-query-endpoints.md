# ADR-0003: Collection Query Endpoints

**Status:** Proposed  
**Date:** 2026-05-28

---

## Context

Each document type in the Trade Gateway API (Intras, CHEDs, DOCOMs) needs an endpoint that returns a paginated list of documents updated within a given time window. Consumers use this to poll for changes incrementally rather than fetching individual documents by ID.

This does not fit the standard REST member-resource pattern (`GET /intras/{id}`) because the request is a query against the collection rather than a retrieval of a known member. Two structural options were considered:

1. **Query parameters on the collection URL** — `GET /intras?after=...&before=...`
2. **A dedicated operation URL** — `GET /intras/_updates?after=...&before=...`

Pagination strategy is constrained by the upstream TracesNT SOAP API, which paginates by page number. The gateway must follow the same model to avoid re-implementing buffering or counting logic in the middle tier.

---

## Decision

### URL structure

Each resource type exposes its own dedicated update-query endpoint using an underscore-prefixed operation segment:

```
GET /intras/_updates?after=2026-01-01T00:00:00Z&before=2026-01-02T00:00:00Z&page=1&pageSize=20
GET /cheds/_updates?after=...&before=...&page=1&pageSize=20
GET /docoms/_updates?after=...&before=...&page=1&pageSize=20
```

The `_` prefix convention (established by Elasticsearch: `_search`, `_bulk`, `_refresh`) signals that the segment is a special operation on the collection, not a resource identifier. This prevents conflicts with real resource IDs and makes it explicit in routes, logs, and metrics that this is a query operation rather than a member retrieval.

Separate per-resource endpoints are used rather than a single unified `/changes?resourceType=...` endpoint because each resource type is handled by its own upstream service. A unified endpoint would require cross-service orchestration for no consumer benefit — consumers polling for updates already know which resource type they care about.

### Query parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `after` | Yes | Return documents updated after this instant (exclusive). ISO 8601 UTC. |
| `before` | Yes | Return documents updated before this instant (inclusive). ISO 8601 UTC. |
| `page` | No | 1-based page number. Defaults to 1. |
| `pageSize` | No | Items per page. Defaults to 20, capped at 100. |

The parameters are named `after` and `before` rather than `updatedAfter` / `updatedBefore` because the `_updates` segment already establishes the temporal context. Repeating "updated" in the parameter names is redundant.

Both `after` and `before` are required on every request. Open-ended time windows are not supported — they would produce unbounded result sets.

### Pagination

Offset-based (page number) pagination is used, mirroring the TracesNT SOAP API. The gateway passes `page` and `pageSize` directly to the upstream call, avoiding any buffering or re-counting logic in the middle tier.

The response envelope:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "totalItems": 92
}
```

`totalPages` and `totalItems` are included when the upstream provides them, allowing consumers to detect when they have retrieved all available items.

### Response content type

The `items` array contains the same representation as `GET /intras/{id}`, subject to the same content negotiation via the `Accept` header (per [ADR-0001](./0001-api-versioning-via-content-negotiation.md)). The envelope itself (`items`, `page`, `pageSize`, `totalPages`, `totalItems`) is unversioned infrastructure.

### Error responses

All errors follow [ADR-0002](./0002-rest-api-conventions.md) — RFC 7807 Problem Details with `application/problem+json`:

| Code | When used |
|------|-----------|
| `400 Bad Request` | `after` or `before` is missing, unparseable, or `after` ≥ `before`. |
| `403 Forbidden` | Caller is not permitted to query this resource type. |
| `502 Bad Gateway` | Upstream service communication failure. |

---

## Consequences

**Positive**

- The `_` prefix cleanly distinguishes operation URLs from resource member URLs with no ambiguity.
- Page-number pagination aligns directly with the TracesNT SOAP API — no impedance mismatch or buffering in the gateway.
- Per-resource endpoints align with the per-service ownership model and keep routing, authorisation, and OpenAPI schemas cleanly separated.
- `after`/`before` naming is concise and unambiguous given the URL context.

**Negative / Trade-offs**

- The `_` prefix convention is non-standard REST and may be unfamiliar; it must be explained in API documentation.
- Page-number pagination can produce inconsistent results if the underlying dataset changes between page requests (items added or removed can shift pages). This is acceptable for the polling use case where eventual consistency across a crawl is tolerable.
- Both `after` and `before` are required — consumers cannot request an open-ended "everything since" query.
