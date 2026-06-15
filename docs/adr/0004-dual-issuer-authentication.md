# ADR-0004: Dual-Issuer Authentication (Cognito + STS)

**Status:** Accepted  
**Date:** 2026-06-10

---

## Context

The Trade Gateway API must authenticate two distinct categories of client:

| Client type | Deployment context | Identity provider |
|---|---|---|
| External clients | Outside CDP | AWS Cognito (User Pool, `client_credentials` flow) |
| Internal CDP services | Deployed to CDP | AWS Security Token Service (STS), IAM role-derived tokens |

A single JWT Bearer scheme bound to one authority cannot validate tokens from the other because the two providers have separate OIDC discovery endpoints and signing key sets. Forcing all clients through Cognito would require CDP-internal services to maintain Cognito credentials, coupling them to an identity plane they do not otherwise use.

---

## Decision

Register two named JWT Bearer schemes (`Cognito` and `Sts`) and a `PolicyScheme` (`MultiIssuer`) as the default authentication scheme. The policy scheme inspects the JWT `iss` claim before the token is validated and forwards the request to the appropriate bearer handler.

```
Request → MultiIssuer (PolicyScheme)
              ├─ iss == StsAuthority  →  Sts (JwtBearer)
              └─ otherwise           →  Cognito (JwtBearer)
```

Each scheme is configured independently with its own `Authority` (OIDC discovery) and `ValidIssuer`. Audience validation is disabled for Cognito: M2M `client_credentials` tokens carry no `aud` claim. STS tokens **do** carry an `aud` claim (set by the calling service to the name of the intended recipient); the `Sts` scheme validates that `aud` equals `Authentication__Sts__Audience` (`"trade-gateway"`), so a token issued for a different service is rejected.

### Configuration

Both schemes are always active. Configuration is required for both regardless of deployment environment:

```
Authentication__Cognito__Authority   – Cognito User Pool OIDC endpoint
Authentication__Cognito__Scope       – Required scope claim value for Cognito tokens
Authentication__Sts__Authority       – STS/IAM Identity Center OIDC endpoint
Authentication__Sts__Audience        – Expected audience value for STS tokens (e.g. "trade-gateway")
```

### Authorization

The `ApiAccess` policy enforces scheme-specific validation:

- A Cognito-authenticated request must carry `Authentication__Cognito__Scope` in its `scope` claim.
- An STS-authenticated request is authorized solely by issuer + signature + audience validation. No scope claim is required or checked.

---

## Consequences

**Positive**

- External and internal clients use the identity plane natural to their deployment context — no cross-plane credential management.
- Issuer routing is transparent to the rest of the application; controllers and policies see a single authenticated `ClaimsPrincipal` regardless of which scheme authenticated it.
- Adding or rotating signing keys at either authority is handled automatically via OIDC discovery — no code changes required.
- Scope requirements are enforced independently per issuer, so the two client populations can have different access grants.

**Negative / Trade-offs**

- Both `Authority` values must be reachable at startup (OIDC discovery is fetched by the backchannel HTTP client on first use). Both must be configured even in environments where only one client type is expected.
- The issuer-routing step reads and partially decodes the JWT before validation. An incoming token with a malformed header will fall through to the Cognito scheme and fail there rather than returning an early 401.
- STS authorization relies on the `aud` claim being set correctly by the calling service. A misconfigured caller that omits or mis-sets the audience will receive a `401` rather than reaching the application.
