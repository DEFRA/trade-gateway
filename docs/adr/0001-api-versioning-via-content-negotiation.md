# ADR-0001: API Versioning via HTTP Content Negotiation

**Status:** Proposed  
**Date:** 2026-05-13

---

## Context

The Trade Gateway API exposes data for many distinct domains and response types. As the API evolves, individual response schemas will change at different rates — a certificate response may need a new field while a consignment response remains stable. We need a versioning strategy that accommodates this reality.

Three approaches were considered:

1. **URL versioning** — embed version in the path (`/v1/intra/{id}`, `/v2/intra/{id}`)
2. **Whole-API versioning** — version the entire API as a unit, deploying a new API version when any resource changes
3. **HTTP content negotiation** — clients request a specific representation using the `Accept` header with vendor media types (`application/vnd.defra.trade.intra.v1+json`)

---

## Decision

We will use **HTTP content negotiation via vendor media types** to version individual resource representations.

Each versioned representation of a resource is identified by a vendor media type:

```
application/vnd.defra.trade.intra.v1+json
application/vnd.defra.trade.intra.v2+json
```

The endpoint URL (`/intra/{id}`) remains stable. Clients declare which representation they want in the `Accept` header. The latest stable version is returned when no `Accept` header is provided or when the header does not match a known vendor media type.

---

## Rationale

### Why not URL versioning

URL versioning (`/v1/resource`, `/v2/resource`) is common but creates coupling problems at scale:

- **The URL is a lie.** A URL is supposed to identify a resource. `/v1/intra/GB123` and `/v2/intra/GB123` identify the same certificate — the version identifies a *representation*, not a different resource. Embedding it in the URL conflates two concerns.
- **Proliferation of routes.** With many domain types across this API, URL versioning produces a combinatorial explosion of routes. Every new version of every resource adds permanent routes that must be maintained indefinitely.
- **Forced co-ordination.** Clients must update base URLs when upgrading, which is a higher-friction change than updating an `Accept` header. Internal consumers and scripts all hardcode paths.
- **Cache fragmentation.** Caches treat `/v1/intra/GB123` and `/v2/intra/GB123` as unrelated resources, reducing hit rates and making cache invalidation harder.
- **No per-resource granularity.** URL versioning typically versions whole controllers or modules together, forcing consumers to respond to changes that may not affect the resources they use.

### Why not whole-API versioning

Versioning the entire API as a unit compounds the URL versioning problems:

- **Big-bang upgrades.** Any change to any resource requires a new API version, forcing all consumers to plan an upgrade regardless of whether they use the changed resource.
- **Long tail of deprecated versions.** The API must run multiple full versions concurrently while all consumers migrate, a significant operational and testing burden.
- **Change frequency mismatch.** Different domains evolve at different rates. A certificate schema change should not block or couple to a consignment schema change.

### Why content negotiation

- **Standards-driven.** HTTP content negotiation (`Accept`, `Content-Type`) is defined in [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110). It is the mechanism HTTP was designed for selecting representations of a resource.
- **Per-resource granularity.** Each domain type carries its own vendor media type. A new version of a certificate response has no effect on consignment consumers. Versions evolve independently.
- **Stable URLs.** Resource identifiers do not change. Bookmarks, logs, and downstream references remain valid indefinitely.
- **Safe default.** The `Accept` header is entirely optional. Clients that omit it receive a chosen default — which could be the latest version, the oldest compatible version, or any other policy appropriate to the resource. New consumers work without any explicit version negotiation; they only need to set the header when they want a representation that differs from the default.
- **Discoverable.** The OpenAPI specification documents all supported content types per endpoint, including their schemas. Clients can introspect exactly what representations are available without out-of-band documentation.
- **Low migration cost.** Moving a consumer from v1 to v2 of a single resource requires changing one header value, not a base URL across an entire client library.

---

## Versioning Semantics

A vendor media type is a **compatibility contract**, not a frozen snapshot of a schema at a point in time.

### Non-breaking changes — no new version required

Additive, backwards-compatible changes are delivered under the existing media type. Clients receive them automatically without updating their `Accept` header or being aware a change happened. Examples:

- Adding a new optional field
- Adding a new optional nested object
- Relaxing a validation constraint (e.g. making a required field optional)

`application/vnd.defra.trade.intra.v1+json` means *"a v1-compatible representation, at the latest revision"*, not *"the exact schema as it was when v1 was first defined"*.

### Breaking changes — new media type required

A new vendor media type is minted only when a change cannot be made without breaking existing consumers. Examples:

- Removing or renaming a field
- Changing a field type
- Making an optional field required
- Restructuring the response shape

The previous media type continues to be served until all consumers have migrated. Retiring an old version is an explicit, communicated decision — it does not happen automatically.

### The Accept header is optional

Sending an `Accept` header is never required. Clients that omit it receive the default version — the API owner decides what that default is (typically the latest, but could be the oldest compatible version for stability). This means:

- **Simple consumers** need no knowledge of versioning at all. They call the endpoint and get a working response.
- **Version-aware consumers** pin to a specific media type when they need a stable, known contract.
- **There is no need for wildcard media types** such as `application/vnd.defra.trade.*+json`. Omitting the header already expresses *"I accept whatever you give me"*, and it is valid HTTP. A custom wildcard pattern is non-standard and provides no additional capability.

---

## Implementation

- Vendor media types are declared on the model class using a `[MediaType(...)]` attribute, keeping the type and its media type co-located.
- Endpoint metadata uses `.Produces<T>(200, mediaType)` for each representation, which is reflected in the OpenAPI specification.
- The OpenAPI specification is served at `/.well-known/openapi/v1/openapi.json` and rendered via ReDoc at `/redoc`.
- The spec is validated with a snapshot test on every build, preventing unintentional schema drift.

---

## Consequences

**Positive**

- Resource URLs are stable and semantically correct.
- Individual domain types version independently — no cross-domain coupling.
- The OpenAPI spec accurately documents all available representations per endpoint.
- Aligns with REST constraints and HTTP standards.

**Negative / Trade-offs**

- Less familiar to consumers accustomed to URL versioning; requires documentation and examples.
- Vendor media types are not validated by the framework; a typo in an `Accept` header silently returns the default version rather than a 406. This should be considered if strict version pinning becomes a requirement.
