# Customs Quantity Mappings

This document describes how SOAP types from the TracesNT customs quantity-management service are mapped to the customs contract types exposed by `CustomsChedQuantityEndpoints`.

---

## The upstream call

`CustomsCertexChedPort.processedChedRequestAsync` (generated). One call returns the CHED's entire quantity position — every allocation, for every declaration.

**Headers**

| SOAP header | Source |
|---|---|
| `Security` | WS-Security UsernameToken, `TracesNt:Credentials:Customs` (a different account from every other port) |
| `WebServiceClientId` | `TracesNt:Credentials:Customs:WebServiceClientId` |
| `LanguageCode` | `Accept-Language` → `AcceptLanguageParser.GetPrimaryLanguageCode(...)`, defaults to `"en"` |
| `CustomsOfficeReferenceNumber` | `TracesNt:CustomsOfficeReferenceNumber` |
| `CertexHeader.MessageId` | per-request `Guid.NewGuid().ToString("N")`, logged with the CHED id |
| `CertexHeader.UniqRequesterPrefix` | `TracesNt:CustomsOfficeReferenceNumber` |

**Body — `ProcessedChedRequestType`**

| Field | Value | Notes |
|---|---|---|
| `SendingDate` | `DateTime.UtcNow` | |
| `ChedCertificateId` | route value `chedId` | |
| `CompetentCustomsOffice.ReferenceNumber` | `TracesNt:CustomsOfficeReferenceNumber` | single-tenant gateway, one office |
| `QuantityManagementIndication` | `"0"` on a read, `"1"` on a reservation | one character between reading and mutating customs state |
| `Language` | primary language code | |
| `CustomsDeclarationReferenceNumber` | **empty** on a read; `{ Item = mrn, ItemElementName = MRN }` on a reservation | the schema marks it optional; TracesNT rejects the request without it. On a read a value would narrow the response to one declaration; on a write it is what the quantities are reserved against |
| `CommodityDescriptionForChed` | *unset* on a read; the requested items on a reservation | `ConsignmentItemR6ForReservationType[]` |
| `PdfGenerationIndicationSpecified` | `false` | otherwise every read renders a PDF |
| `TransformationIndictionSpecified` | `false` | |
| `PushIndication` | *unset* | the gateway does not subscribe to updates |
| `TARICDocument`, `CommodityDescriptionForChed` | *unset* | write-path fields |

---

## Endpoints

### `GET /customs/cheds/{chedId}/quantities` — `ProcessedChedInformationResponseType` → `ChedQuantityLedger`

| Target field | Source path | Notes |
|---|---|---|
| `available[]` | `QuantityManagementSummary.AvailableQuantity` | see [AvailableCommodityQuantity](#availablecommodityquantity--productquantityenhancedplusplustype) |
| `allocations.reserved[]` | `QuantityManagementSummary.ReservedQuantity` | see [AllocatedCommodityQuantity](#allocatedcommodityquantity--allocatedproductquantitybycustomsofficeenhanced4chedr51type) |
| `allocations.consumed[]` | `QuantityManagementSummary.ConsumedQuantity` | |

| Upstream response | Status |
|---|---|
| no `ChedCertificate` | **404** — TracesNT's way of saying the CHED does not exist |
| `ChedCertificate` present, `QuantityManagementSummary == null` | **502** — the CHED is there, its position is not |
| upstream fault | **502** |

Never 200 with an empty ledger: absent and empty are identical on the wire.

The ledger carries **no CHED id and no timestamp**. Both would restate the request: the caller supplied the id in the URL, and the `Date` response header says when it was read. Neither is upstream data — the id would be an echo of the route value rather than TracesNT's, and a timestamp would record when the gateway mapped the response, not when the figure was true. A caller persisting the payload as evidence records both itself.

There is no per-declaration endpoint. `allocations.reserved[]` and `allocations.consumed[]` cover every declaration holding against the CHED, each entry carrying its own `declarationReference` — consumers interested in one MRN filter on that. **Match on `declarationReference.type == "MRN"` as well as the value**: an LRN can carry the same string as an MRN and is a different declaration. See [ADR-0006](../adr/0006-customs-quantity-management.md).

### `PUT /customs/cheds/{chedId}/declarations/{mrn}/reservation`

**Request** — `ChedReservationRequest.items[]` → `ProcessedChedRequestType.CommodityDescriptionForChed` (`ConsignmentItemR6ForReservationType[]`)

| Source field | Target field | Notes |
|---|---|---|
| `goodsItemNumber` | `GoodsItemNumber` | `xs:integer` as string; required — see below |
| `certificateLineNumber` | `CertificateLineNumber` | as above; required — see below |
| `classCode` | `ClassCode` | required — see below |
| `netWeightQuantity` | `NetWeightQuantity` + `NetWeightQuantitySpecified` | |
| `netWeightUnitOfMeasure` | `NetWeightUnitOfMeasure` + `NetWeightUnitOfMeasureSpecified` | enum name |
| `netVolumeQuantity` | `NetVolumeQuantity` + `NetVolumeQuantitySpecified` | |
| `netVolumeUnitOfMeasure` | `NetVolumeUnitOfMeasure` + `NetVolumeUnitOfMeasureSpecified` | enum name |

**Every value has a `Specified` companion, and every companion is set from whether the field was supplied.** A value whose companion stays `false` is dropped from the request without error — reserving less than the caller asked for, successfully.

Rejected with **400** before anything is sent: an item with neither quantity, a quantity with no unit of measure, or a unit that is not a `UniversalUnitOfMeasureType` member. All three would otherwise reach TracesNT with the unit element absent, to be recorded against upstream's default of tonnes.

Also **400**: an item missing any of `goodsItemNumber`, `certificateLineNumber` or `classCode`. TracesNT's schema requires all three elements.

**Response** — `ProcessedChedInformationResponseType` → `ChedDeclarationReservation`

| Target field | Source path | Notes |
|---|---|---|
| `reserved[]` | `QuantityManagementSummary.ReservedQuantity` | filtered to the requested MRN |
| `consumed[]` | `QuantityManagementSummary.ConsumedQuantity` | filtered to the requested MRN |

The filter matches `declarationReference.type == MRN`

| Upstream response | Status |
|---|---|
| no `ChedCertificate` | **404** |
| `ReservationResultSpecified` and `ReservationResult` true | **200** |
| `ReservationResultSpecified` and `ReservationResult` false | **409** — `failureReason` carries the decoded `{code, description}`, `failedItem` the goods item and document line numbers |
| `ReservationResultSpecified` false | **502** |
| reserved, but no `QuantityManagementSummary` | **502** |
| reserved, but nothing matches the MRN | **502** |
| upstream fault | **502** |

Never 200 with empty arrays: that would deny the reservation the same response just confirmed.

---

## Types

### `AvailableCommodityQuantity` ← `ProductQuantityEnhancedPlusPlusType`

| Target field | Source path | Notes |
|---|---|---|
| `commodityCode` | `CommodityCode` | `null` when every part is absent |
| `certificateLineNumber` | `SwSupportingDocument.CertificateLineNumber` | `xs:integer` as string; unparseable → `null`, never an exception |
| `unitOfMeasure` | `SwSupportingDocument.UnitOfMeasure` | enum name. `null` **only** when `SwSupportingDocument` itself is absent — an absent *element* deserialises to `TNE`. |
| `quantity` | `SwSupportingDocument.Quantity` | plain `decimal`; absent → `0` |

### `AllocatedCommodityQuantity` ← `AllocatedProductQuantityByCustomsOfficeEnhanced4ChedR51Type`

| Target field | Source path | Notes |
|---|---|---|
| `goodsItemNumber` | `GoodsItemNumber` | `xs:integer` as string; unparseable → `null` |
| `commodityCode` | `CommodityCode` | `null` when every part is absent |
| `certificateLineNumber` | `SwSupportingDocument.CertificateLineNumber` | as above |
| `unitOfMeasure` | `SwSupportingDocument.UnitOfMeasure` | as above |
| `quantity` | `SwSupportingDocument.Quantity.Value` | the source wraps this in a complex type; the contract flattens it so `quantity` means the same thing on both shapes |
| `technicalRoundingQuantity` | `SwSupportingDocument.Quantity.TechnicalRoundingQuantity` | honours `TechnicalRoundingQuantitySpecified`; unspecified → `null`, **not** `0` |
| `eventDateTime` | `EventDateTime` | honours `EventDateTimeSpecified`; unspecified kind treated as UTC |
| `customsOffice` | `CompetentCustomsOffice.ReferenceNumber` | |
| `declarationReference` | `Item` + `ItemElementName` | see below |

### `CommodityCode` ← `CommodityCodeEnhanced4AvailableType` / `CommodityCodeEnhancedType`

| Target field | Source path |
|---|---|
| `harmonizedSystemSubheadingCode` | `HarmonizedSystemSubheadingcode` |
| `combinedNomenclatureCode` | `CombinedNomenclatureCode` |
| `taricCode` | `TARICCode` |

Mapped to `null` in its entirety when all three are absent, rather than to an object of nulls.

### `DeclarationReference` ← `Item` / `ItemElementName`

| Target field | Source path | Notes |
|---|---|---|
| `type` | `ItemElementName` | `MRN` only when the discriminator says so; **anything else is `LRN`** |
| `value` | `Item` | |

`null` when `Item` is null or empty. `ItemChoiceType2` defaults to `LRN` at index 0, so a value arriving without a discriminator is read as the LRN it claims to be — never promoted to an MRN. This is what makes it safe for a consumer to narrow the ledger to a single declaration.
