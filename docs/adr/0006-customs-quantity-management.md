# ADR-0006: Customs Quantity Management

**Status:** Proposed  
**Date:** 2026-08-03

---

## Context

The EU's CERTEX quantity-management flow lets a customs authority reserve quantities from a CHED against a customs declaration, then write those quantities off — or roll them back — when the goods are cleared. TracesNT exposes it through a single SOAP port, `CustomsCertexChedServiceV06`.

Three properties of that port shape every decision below:

- **One operation serves both reads and writes.** `processedChedRequest` reserves quantities when `QuantityManagementIndication = "1"` and only reports them when it is `"0"`.
- **There is exactly one fault type,** carrying free text and nothing machine-readable. Every upstream failure arrives identically.
- **Absence and emptiness are identical on the wire.** The quantity summary's allocation lists are unwrapped repeated elements, so "no allocations" and "allocations not reported" cannot be told apart.

Behaviour cited as "the guidelines" is *EU CSW-CERTEX Guidelines (IT Release 4.1)*, v11.00, 20/03/2023 — §3.2 quantity management, §5.4 CHED.

Field-level mapping detail lives in [docs/mappings/customs-quantity-mappings.md](../mappings/customs-quantity-mappings.md); this ADR records only the decisions.

---

## Decision

### The feature is called "customs", not "CERTEX"

CERTEX names a supplier's product on the other end of the wire. It means nothing to a Defra reader, and if DG SANTE renames the flow every type in the gateway would be misnamed.

**Everything we write is named `Customs`; `Certex` appears only where it is not ours to choose** — generated clients, the service path, and SOAP namespaces, all of which are produced by `scripts/update-webservices.sh` or fixed by the wire format, so renaming them is impossible or would be undone on the next regeneration.

This covers services, exceptions, endpoints, contract namespaces, config keys and authorisation principals. Prose still says CERTEX when naming the EU system, its guidelines or its schema.

### Endpoints

```
GET customs/cheds/{id}/quantities                         the CHED's whole quantity position
PUT customs/cheds/{id}/declarations/{mrn}/reservation      reserve against one declaration
PUT customs/cheds/{id}/declarations/{mrn}/release      releases the reserved quantity against one declaration
```

The read returns the entire CHED position — every allocation, for every declaration. A per-declaration read (`GET .../reservation`) is **not** implemented: upstream has one operation returning everything, so a second URL would issue the identical call and discard most of the answer. It adds a filter, not information. Consumers interested in one declaration filter the ledger themselves.

The write is `PUT` rather than `POST` because the upstream operation states the declaration's whole position rather than adding to it — sending the same body twice leaves the same reservation. Unlike the read, its response **is** narrowed to the requested declaration, because only the gateway sees the MRN/LRN discriminator.

`POST .../clearance` and `DELETE .../reservation` are not built — see "Still to build".

The `customs/` prefix, and why it sits beside `/certificates/**` rather than beneath it, is [ADR-0005](./0005-fine-grained-authorisation.md). REST conventions are [ADR-0002](./0002-rest-api-conventions.md); the singular `reservation` segment does not violate its plural rule, since a declaration has exactly one reservation against a given CHED.

### Ambiguity is a 502, never a confident answer

| Upstream response | `GET .../quantities` | `PUT .../reservation` |
|---|---|---|
| No `ChedCertificate` | **404** | **404** |
| Reservation succeeded | — | **200** + the declaration's reservation |
| Reservation refused | — | **409** + decoded `failureReason` |
| Present but the quantity summary is absent, or answers a question we did not ask | **502** | **502** |
| Upstream fault | **502** | **502** |

TracesNT answers for an unknown CHED with a *successful* response carrying no `ChedCertificate`. That is the port's only not-found signal and it is what licenses the 404. A *fault* stays a 502: the single untyped fault gives nothing to discriminate on, so a 404 there would assert the CHED does not exist when all we know is that the call failed.

Returning 200 with an empty ledger when the summary is absent would assert "nothing is reserved against this CHED" on no evidence. At the customs clearance layer that is a data-integrity failure. 502 says "we do not know", which is true. The same principle governs the write: a success carrying no position, or one holding nothing for the declaration just reserved against, contradicts itself.

### The read cannot mutate customs state

`QuantityManagementIndication` `"0"` reads and `"1"` reserves — one character between a safe read and a state change. Both are named constants, and both paths assert on the outbound SOAP body, because nothing in a *successful* response distinguishes a read from a reservation.

### The reservation write is never retried

Because the upstream operation states the declaration's whole position, a repeat of the same body for the same MRN cannot double-allocate — it lands in the same place. The gateway still does not retry, for a different reason: it cannot tell a request that failed before reaching TracesNT from one that succeeded and lost its response, and whether restating the position is still the right thing to do is the caller's judgement, not the gateway's. The CHED's position may have moved between attempts, and a silent retry would re-assert an intent the caller may have abandoned.

There is no resilience layer today, and this is a decision rather than an oversight: **a blanket retry policy must not be added later without revisiting it**, because it would silently cover this endpoint too.

### A reservation is validated before anything is sent

Any field TracesNT requires is checked before the call, not diagnosed from the fault afterwards — an omitted mandatory field is refused upstream as a sender fault, which would tell the caller the gateway broke on a request only they can fix.

**A quantity is never sent without its unit.** An omitted or misspelled unit is not a smaller reservation, it is a reservation silently recorded in tonnes. An item with no quantity, or a quantity with no unit, is a **400**.

**Weight and volume are not mutually exclusive**, and validation must not be tightened to make them so. The guidelines say net mass *"and/or"* supplementary unit, and the generated type agrees: the elements are independent, with no choice construct. The rule is therefore **at least one**, not exactly one.

In practice we expect customs to send only one of the two, so the permissive rule may never earn its keep. It is kept anyway because the asymmetry is one-sided — accepting both costs nothing if both never arrive, whereas enforcing exactly-one would reject a request the specification allows, and that failure would only show up once some consumer sent one. A business rule refusing a particular combination for a particular CHED arrives as a **409**, not something the gateway can predict.

### Upstream text never reaches a response body

The upstream fault's only content is free text of unknown provenance, so it is logged and mapped to a fixed 502 problem detail ([ADR-0002 §4](./0002-rest-api-conventions.md)).

On a 409, `ReservationFailureReason` carries a code. **Only a code in the known table is published**, as `{ code, description }` beside the failed item; anything else is reported as `code: null` with a generic description, so the caller learns a reason was given and could not be decoded. The raw value is logged with the CHED, the MRN and the upstream `MessageId`, which is how a missing code gets found. The no-echo rule is the one to preserve if the table is extended.

Every outbound request carries a fresh `MessageId`, logged with the CHED id. DG SANTE ask for it when a call is queried, and it is the only correlation handle that exists.

### The MRN/LRN discriminator is always checked

The request and response choice types both default to `LRN`. A declaration reference is therefore an MRN **only** when the discriminator says so — on the read, so consumers cannot match an LRN carrying the same characters as an MRN; on the write's narrowing; and outbound, where forgetting it would reserve against a different declaration. All three are held by tests.

### The ledger carries no id and no timestamp

A `chedId` would echo the route value the caller just supplied rather than TracesNT's own identifier. A `retrievedAt` would record when the gateway mapped the response rather than when the figure was true upstream, and the HTTP `Date` header carries that more honestly. The reference-data contracts' `retrievedAt`/provenance envelope is a convention for slow-moving cacheable data and is not copied here.

A caller persisting a ledger as evidence of a clearance decision must record the CHED and the time itself. The gateway never caches.

---

## Open: the customs office is deployment-wide

`TracesNt:CustomsOfficeReferenceNumber` is one value for the whole gateway. On a read, a wrong value is a wrong request header. **On the reservation write it is a mis-stated fact in customs records** — every reservation is attributed to that office.

**Confirm the gateway fronts exactly one customs office before promoting the write beyond a test environment.** If it fronts several, the office belongs on the request and the configuration value should be removed rather than kept as a default, which would silently attribute reservations to the wrong office. Moving it onto the request is a breaking change, so it is much cheaper to settle now, while there are no callers.

---

## Still to build

- `GET .../reservation` — now worth reconsidering as the natural way to read back what the `PUT` created. Still optional: the whole-CHED read carries the same data, and the `PUT` already returns the resulting reservation.
- `POST .../clearance` → `chedClearanceRequest`. `QuantityManagementOutcome` is an unenumerated two-digit string, so the mapping must have a **`default → 502`** arm. New codes can appear without warning and must never fall through to success.
- `DELETE .../reservation` maps to the same upstream call as a clearance with outcome "not released". Two resource-level expressions of one operation is acceptable, but the alternative reading — a reservation with zero quantities — should be confirmed before building it.
- **No idempotency layer** for repeated clearance. A repeat yields a 409, which is safe and honest.

---

## Consequences

**Positive**

- Read endpoints cannot mutate customs state, and a test on the outbound body enforces it.
- Upstream ambiguity surfaces as 502 rather than a confident wrong answer.
- The `customs/` prefix keeps certificate readers away from customs data by construction.

**Negative / Trade-offs**

- An unknown CHED returns 404 on the strength of the empty-`ChedCertificate` signal. If that signal ever means something else — a permission boundary, a CHED invisible to the customs account — the 404 is a confident wrong answer. Worth re-checking if 404s appear for CHEDs known to exist.
- The tonnes and zero defaults remain latent in the generated client. They are documented and tested, not fixed.
- If TracesNT ever stopped reporting allocations on a read, the ledger would degrade silently into "nothing is reserved". Absence and emptiness are identical on the wire, so no field in the response can signal it and detection would have to come from monitoring.
- Consumers interested in one declaration filter the ledger themselves — a small burden on every consumer, and it depends on them respecting the reference type rather than matching on the value alone.
- The gateway can now change customs state, which is why the write is unretried by design and why `WRITE` is held by a separate principal from `READ`.
- A refused reservation carries a decoded `failureReason`, but only for known codes. Anything else must still be escalated to a human, and is only discoverable from the logs.
