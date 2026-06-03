# ADR-0002: REST API Conventions

**Status:** Proposed  
**Date:** 2026-05-28

---

## Context

The Trade Gateway API will expose multiple document types — Intras, CHEDs, DOCOMs — through a consistent HTTP interface. As the API grows, implicit conventions become ambiguous. We need a documented baseline that all endpoints follow, so that consumers can reason about the API without per-endpoint documentation and so that future endpoints can be added without relitigating the same decisions.

---

## Decision

The following conventions apply to all Trade Gateway endpoints.

### 1. Plural resource names in URLs

Resource collections are identified by plural nouns:

```
GET /intras/{id}
GET /cheds/{id}
GET /docoms/{id}
```

A URL identifies a resource. The collection that contains the resource is plural; the specific member is selected by `{id}`. Using a singular noun (`/intra/{id}`) conflates the collection with its members and breaks the conceptual model of REST.

The `{id}` segment is the business identifier for the resource (e.g. a certificate reference number), not a database key.

### 2. HTTP methods

| Method | Meaning |
|--------|---------|
| `GET` | Retrieve a representation of a resource. Must be safe and idempotent — no side-effects. |

Only `GET` is used in the current API. Additional methods (`POST`, `PUT`, `DELETE`) will be decided per-endpoint if needed and will be added to this ADR at that time.

### 3. HTTP status codes

| Code | When used |
|------|-----------|
| `200 OK` | The resource was found and returned. |
| `400 Bad Request` | The client sent a structurally invalid request. |
| `403 Forbidden` | The client is authenticated but not permitted to access this resource. |
| `404 Not Found` | No resource matching the given identifier exists. |
| `500 Internal Server Error` | An unexpected error occurred within the gateway. |
| `502 Bad Gateway` | The gateway could not communicate with the upstream service (TracesNT). |

No `204 No Content` is returned for retrieval endpoints. If a resource is not found, `404` is used.

### 4. Error responses — RFC 7807 Problem Details

All non-2xx responses use the [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) Problem Details format with content type `application/problem+json`:

```json
{
  "type": "about:blank",
  "title": "Not Found",
  "status": 404,
  "detail": "Intra certificate 'GB123' was not found."
}
```

Error response bodies must not expose upstream implementation details. TRACES exception types, SOAP fault codes, stack traces, and internal identifiers must not appear in any error response — they belong in structured logs only.

### 5. Content negotiation and versioning

Per [ADR-0001](./0001-api-versioning-via-content-negotiation.md), representation versions are selected via the `Accept` header with vendor media types:

```
Accept: application/vnd.defra.trade.intra.v1+json
```

Omitting the `Accept` header returns the latest version. Error responses always use `application/problem+json` regardless of the requested representation version.

### 6. Localisation

The `Accept-Language` header selects the language for human-readable text fields. If omitted or unrecognised, the response defaults to English (`en`). The language code is forwarded to TracesNT as the `ISO2AlphaLanguageCode`.

---

## Consequences

**Positive**

- Consistent URL shape across all document types; consumers can predict endpoint structure.
- All error responses are machine-readable; clients can handle them uniformly.
- No TRACES internals in error responses; safe to surface to external consumers.
- Conventions are explicit and documented; future endpoints have a clear baseline.

**Negative / Trade-offs**

- RFC 7807 `detail` strings are informational but not machine-actionable; clients that need structured error data (e.g. field-level validation errors) will require an extension field if that becomes necessary.
