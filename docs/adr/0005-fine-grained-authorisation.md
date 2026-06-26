# ADR-0005: Fine-Grained Authorisation

**Status:** Proposed  
**Date:** 2026-06-16

---

## Context

The API currently has a single coarse-grained authorisation policy (`ApiAccess`) that grants any authenticated Cognito or STS principal access to all endpoints. The certificate and reference-data endpoints are effectively public to any valid token holder.

This violates the **Principle of Least Privilege (PoLP)**: every service client has more access than it needs. A service that only reads CHED certificates can also read reference data, intra certificates, or any future endpoints added to the API. If a client is compromised, the blast radius is the entire API surface rather than the narrow set of resources that client legitimately needs.

Different service clients require narrowly-scoped access: one service may need read-only access to all certificate types, another may need write access to CHED certificates only. There is no mechanism to express or enforce these distinctions today. The goal of this ADR is to establish an authorisation model that enforces PoLP — each principal is granted the minimum access required to perform its function, and nothing more.

---

## Decision

Introduce a **per-principal, per-resource, per-action** authorisation layer that sits downstream of the existing `ApiAccess` authentication policy:

```
Request → Authentication → ApiAccess (scheme + scope) → Fine-grained authz → Handler
```

A principal must still pass `ApiAccess` first. The fine-grained layer only evaluates resource and action permissions for already-authenticated requests.

### Principals

Every principal — whether Cognito or STS — must have an explicit entry in the authorisation configuration. There is no implicit access for STS principals; IAM-level scoping is not considered sufficient for this API's access model.

The JWT `sub` claim is the canonical principal identifier at runtime. Because raw `sub` values are not human-readable (a Cognito `sub` is a UUID; an STS `sub` is an IAM role ARN), the config uses a **human-readable alias** as the key, with an explicit `sub` binding:

```
Authorization__Principals__<alias>__Sub = <jwt-sub-claim-value>
```

The alias is a stable label chosen by the team (e.g. `ched-importer`, `reference-data-reader`). It is used only in config and logging; only the `sub` is evaluated at runtime.

### Action Model

Two actions are defined:

| Action | HTTP Methods |
|--------|-------------|
| `READ` | `GET` |
| `WRITE` | `POST`, `PUT` |

Any HTTP method not in this table (`PATCH`, `DELETE`, `HEAD`, `OPTIONS`) is not currently modelled and is denied by default. The model must be extended explicitly if such methods are added to the API.

> **Note:** No `WRITE` endpoints exist today. WRITE is defined in the model now to avoid a breaking change to configuration when write endpoints are introduced. Any WRITE permission granted in config has no runtime effect until a corresponding endpoint exists.

### Resource Paths and Wildcard Matching

Permissions are expressed as URL path patterns. Two wildcard operators are supported:

| Operator | Meaning | Example | Matches |
|----------|---------|---------|---------|
| `*` | Any single path segment (no `/`) | `/certificates/*/detail` | `/certificates/ched/detail` but not `/certificates/ched/123/detail` |
| `**` | Any path suffix (zero or more segments) | `/certificates/**` | `/certificates/`, `/certificates/ched`, `/certificates/ched/123` |

Path matching is **case-insensitive**. Trailing slashes are normalised before matching.

### Configuration Format

The configuration is split into two sections with different lifetimes:

- **`Principals`** — maps alias → JWT `sub` value. Environment-specific: the same alias resolves to a different `sub` in dev, staging, and production.
- **`Permissions`** — maps alias → list of resource/action grants. Environment-agnostic: the same permissions apply across all environments and are checked into source control.

The combined structure expressed as JSON (used locally and in tests via `appsettings.json`):

```json
{
  "Authorization": {
    "Principals": {
      "ched-importer":        "arn:aws:sts::123456789012:assumed-role/ched-importer-role/session",
      "reference-data-reader": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    },
    "Permissions": {
      "ched-importer": [
        { "Actions": ["READ", "WRITE"], "Resource": "/certificates/ched/**" },
        { "Actions": ["READ"],          "Resource": "/certificates/intra/**" }
      ],
      "reference-data-reader": [
        { "Actions": ["READ"], "Resource": "/reference-data/**" }
      ]
    }
  }
}
```

In production, CDP injects configuration exclusively as **environment variables**. Because `Principals` is a flat alias → string dictionary, its env vars are simple key=value pairs with no array indexing:

```
Authorization__Principals__ched-importer=arn:aws:sts::123456789012:assumed-role/ched-importer-role/session
Authorization__Principals__reference-data-reader=b2c3d4e5-f6a7-8901-bcde-f12345678901
```

`Permissions` is stable across environments and can be supplied via `appsettings.json`. If it must also be expressed as environment variables (e.g. for a fully env-driven deployment), ASP.NET Core requires zero-based integer indices for arrays:

```
Authorization__Permissions__ched-importer__0__Actions__0=READ
Authorization__Permissions__ched-importer__0__Actions__1=WRITE
Authorization__Permissions__ched-importer__0__Resource=/certificates/ched/**
Authorization__Permissions__ched-importer__1__Actions__0=READ
Authorization__Permissions__ched-importer__1__Resource=/certificates/intra/**
```

The verbosity of the `Permissions` env var form is a consequence of the platform constraint, not the data model. Indices must be zero-based and contiguous — a gap in the sequence causes the remainder to be silently dropped by the ASP.NET Core binder.

Startup validation should fail fast if any alias present in `Permissions` has no corresponding entry in `Principals`, or if any principal entry has an empty permissions list.

#### Trade-off: appsettings.json vs CDP config repo for Permissions

ASP.NET Core merges environment variables over `appsettings.json`, so `Principals` can live in the CDP config repo and `Permissions` in `appsettings.json` without conflict. The natural placement for `Permissions` is therefore `appsettings.json` — it is stable, environment-agnostic, and avoids the indexed array format entirely.

However, `appsettings.json` lives in a **public GitHub repository**. Committing `Permissions` there would expose which service clients have access to which resources and actions — information that could assist an attacker in scoping a compromise.

If the permission model is considered sensitive, `Permissions` should be moved to the CDP private config repo and expressed as environment variables, accepting the verbosity cost. The decision should be made deliberately:

| | `appsettings.json` (public repo) | CDP config repo (env vars) |
|---|---|---|
| Readability | JSON — easy to review in PRs | Indexed env var format — harder to audit |
| Change process | Code PR | Config repo PR (separate pipeline) |
| Visibility | Public | Private |
| Exposure risk | Permission model is public | Permission model is private |

### Evaluation Algorithm

For a given request (`principal sub`, `path`, `HTTP method`):

1. Resolve the action (`READ` / `WRITE`) from the HTTP method. If the method is not mapped, deny.
2. Look up the principal entry in config by `sub` claim value.
3. If no entry exists → **403 Forbidden**.
4. For each permission in the principal's entry:
   - If the request action is in `Actions`, **and** the request path matches `Resource` → **allow**.
5. If no permission matched → **403 Forbidden**.

### Denial Behaviour

All denials return **403 Forbidden**. Unauthenticated requests continue to return **401 Unauthorized** from the existing `ApiAccess` policy.

### Exclusions

The following paths bypass fine-grained authorisation:

| Path | Reason |
|------|--------|
| `/health` | Infrastructure health probes must not require credentials |
| `/.well-known/openapi/**` | API documentation is public |
| `/redoc` | API documentation is public |

---

## Open: Resource Taxonomy

The resource path patterns used in permissions depend on the API's URL structure, which is an **unresolved team decision**.

**Option A — Restructure routes to match a resource hierarchy**

Adopt a hierarchical route structure that makes grouping explicit in the URL:

```
/certificates/intra          (currently: /intras)
/certificates/intra/{id}     (currently: /intras/{id})
/certificates/ched/**        (future)
/reference-data/classifications/sections    (currently: /classificationSections)
/reference-data/classifications/trees/{id} (currently: /classificationTrees/{id})
/reference-data/metadata/{type}            (currently: /metaDatas/{type})
```

Permission rules become self-explanatory: `READ /certificates/**`, `READ /reference-data/**`. The URL directly reflects the resource taxonomy the authorisation model is built on.

**Trade-off:** This is a breaking change to existing route paths.

---

**Option B — Keep existing routes; define a logical mapping layer**

Retain current URLs. A separate config section maps symbolic resource names to actual route patterns:

```
Authorization__Resources__certificates.intra=/intras/**
Authorization__Resources__reference-data=/classificationSections,/classificationTrees/**,/metaDatas/**
```

Permissions reference the symbolic name rather than a URL path.

**Trade-off:** Two things to maintain. The mapping layer can diverge from reality silently.

---

*This section should be resolved before this ADR is accepted.*

---

## Alternatives Considered

### Role-Based Access Control (RBAC)

A role-based approach inverts the model: endpoints declare a required role in code, and config assigns roles to principals.

Roles are named labels defined by the team (e.g. `CertificateReader`, `ChedManager`). Each endpoint is annotated with the role(s) that can access it, and config maps principals to roles:

```json
{
  "Authorization": {
    "Roles": {
      "ched-importer": ["ChedManager", "IntraReader"],
      "reference-data-reader": ["ReferenceDataReader"]
    }
  }
}
```

The env var format is also simpler — a flat role list rather than nested permissions with actions and resources:

```
Authorization__Roles__ched-importer__0=ChedManager
Authorization__Roles__ched-importer__1=IntraReader
```

RBAC is a natural fit for **user-facing systems** where a human may hold several roles simultaneously (e.g. a user who is both a Viewer and an Auditor), and where the IDP can be the authoritative source of role assignments — meaning the application receives roles as claims in the token without needing its own config.

Neither of those conditions holds here. CDP does not support M2M role assignment in Cognito or STS — roles cannot be attached to a service client in the IDP and surfaced as token claims. The role-to-principal mapping would therefore still need to be maintained in application config, eliminating the primary operational advantage of RBAC. We would carry the complexity of a role model without the benefit of centralised role management.

Additionally, this API uses **M2M authentication**. Service clients are not multi-role users — each client has a single, well-defined function: a CHED importer imports CHEDs, a reference data reader reads reference data. Assigning a service client multiple roles is a sign the client is doing too much, or that the roles are too fine-grained to be useful. The M2M pattern aligns more naturally with a client being granted explicit, narrow access to the specific resources it needs — which is what the path-based model expresses directly.

The path-based approach also keeps the full access model in config. Changing which resources a principal can access requires only a config change, with no code deployment. RBAC splits this: role assignment lives in config, but the resource-to-role mapping is baked into endpoint annotations, meaning adding a new endpoint without assigning a required role is easy to miss.

| | Path-based (this ADR) | Role-based |
|---|---|---|
| Access model lives in | Config entirely | Split: endpoint annotations + config |
| M2M fit | Direct — grant exactly what's needed | Indirect — requires role-per-function design |
| Adding a new principal | Config change only | Config change only |
| Adding a new endpoint | Config change to grant access | Code change to annotate required role |
| Default-deny safety | Enforced by model | Requires discipline in endpoint annotation |
| Env var complexity | Higher (nested actions + resource) | Lower (flat role list) |

RBAC was rejected in favour of the path-based model primarily because this is an M2M API: the single-function nature of service clients makes explicit resource grants a more honest and auditable representation of intent than role assignment.

---

## Consequences

**Positive**

- All access to the API is explicitly granted; no principal has implicit broad access by virtue of holding a valid token.
- Config is in environment variables, consistent with the CDP platform approach. Changes go through the normal deployment pipeline and are tracked.
- The alias/sub separation keeps config readable without coupling it to opaque identity provider values.
- Both Cognito and STS principals are modelled identically, providing a single place to audit all access across both identity schemes.

**Negative / Trade-offs**

- Environment variable array notation (`__0__`, `__1__`) is verbose and error-prone for principals with multiple permissions. Startup validation is essential to catch misconfiguration early.
- `sub` values are environment-specific, requiring per-environment configuration of the same logical principals. Aliases provide consistency but the binding must be maintained across environments.
- Adding a new principal currently requires a deployment. There is no mechanism to grant access at runtime without restarting the application.
- No WRITE endpoints exist today; the WRITE action in config is dormant until write endpoints are built.
