# Certificate Mappings

This document describes how SOAP types from the TracesNT service are mapped to the Defra contract types. Shared type mappings are reused across all certificate types.

---

## Certificates

### Ched — `ChedCertificateType` → `DefraUNVTDCHEDProfile`

| Target field | Source path | Notes |
|---|---|---|
| `$model` | `"defra/certificate-internal/1"` | const default |
| `$type` | `"ched"` | const default |
| `exchangedDocument` | `SPSCertificate.SPSExchangedDocument` |  |
| `specifiedConsignment[0]` | `SPSCertificate.SPSConsignment` | single-item list; see [Consignment](#consignment--spsconsignmenttype) |

### Intra — `EuIntraCertificateType` → `DefraUNVTDINTRAProfile`

| Target field | Source path | Notes |
|---|---|---|
| `$model` | `"defra/certificate-internal/1"` | const default |
| `$type` | `"intra"` | const default |
| `exchangedDocument.documentTypeCode` | `SPSCertificate.SPSExchangedDocument.TypeCode.Value` | profile-specific type `DefraUNVTDINTRAProfileExchangedDocument`; only `documentTypeCode` is mapped |
| `specifiedConsignment[0]` | `SPSCertificate.SPSConsignment` | single-item list; see [Consignment](#consignment--spsconsignmenttype) |
| `laboratoryObservationResult` | `null` | `SPSConsignmentItemLaboratoryTest` not mapped in v1 |

---

## Shared Types

### `ExchangedDocument` ← `SPSExchangedDocumentType`

| Target field | Source path | Notes |
|---|---|---|
| `name` | `Name[].Value` | [language-preferred](#language-selection) |
| `identifier` | `ID.Value` | required |
| `documentTypeCode` | `TypeCode.Value` | e.g. `"856"` |
| `documentStatusCode` | `StatusCode.Value` | e.g. `"1"` |
| `issueDateTime` | `IssueDateTime.Item` | ISO 8601; see [Date/Time Handling](#datetime-handling) |
| `includedNote` | `IncludedSPSNote[]` | see [IncludedNote](#includednote--spsnotetype); omitted if empty |
| `referenceDocument` | `ReferenceSPSReferencedDocument[]` | see [ReferencedDocument](#referenceddocument--spsreferenceddocumenttype); omitted if empty |
| `firstSignatoryAuthentication` | `SignatorySPSAuthentication` where `TypeCode = 4` (Inspection) | see [Authentication](#authentication--spsauthenticationtype); matched by type code, not position |
| `secondSignatoryAuthentication` | `SignatorySPSAuthentication` where `TypeCode = 1` (Clearance) | |
| `thirdSignatoryAuthentication` | `SignatorySPSAuthentication` where `TypeCode = 8` (Container inspection) | |

---

### `Consignment` ← `SPSConsignmentType`

| Target field | Source path | Notes |
|---|---|---|
| `availabilityDueDateTime` | `AvailabilityDueDateTime.Item` | ISO 8601 |
| `exportExitDateTime` | `ExportExitDateTime.Item` | ISO 8601 |
| `consignorParty` | `ConsignorSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `consigneeParty` | `ConsigneeSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `despatchParty` | `DespatchSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `customsTransitAgentParty` | `CustomsTransitAgentSPSParty` |  see [TradeParty](#tradeparty--spspartytype) |
| `exportCountry` | `ExportSPSCountry` | see [TradeCountry](#tradecountry--spscountrytype) |
| `importCountry` | `ImportSPSCountry` | see [TradeCountry](#tradecountry--spscountrytype) |
| `reExportCountry` | `ReExportSPSCountry[]` | list; omitted if empty, see [TradeCountry](#tradecountry--spscountrytype) |
| `transitCountry` | `TransitSPSCountry[]` | list; omitted if empty, see [TradeCountry](#tradecountry--spscountrytype) |
| `unloadingBaseportLocation` | `null` | not mapped in v1 |
| `includedConsignmentItem` | `IncludedSPSConsignmentItem[]` | see [ConsignmentItem](#consignmentitem--spsconsignmentitemtype); omitted if empty |

---

### `TradeParty` ← `SPSPartyType`

| Target field | Source path | Notes |
|---|---|---|
| `identifier` | `ID.Value` | |
| `name` | `Name.Value` | |
| `partyRoleCode` | `RoleCode.Value` | e.g. `"VJ"` |
| `partyTypeCode` | `TypeCode[0].Value` | first entry |
| `postalAddress` | `SpecifiedSPSAddress` | see [TradeAddress](#tradeaddress--spsaddresstype) |
| `definedContact[0].personName` | `SpecifiedSPSPerson.Name.Value` | single person; omitted if no person |

---

### `TradeAddress` ← `SPSAddressType`

| Target field | Source path | Notes |
|---|---|---|
| `postcodeCode` | `PostcodeCode.Value` | |
| `lineOne` | `LineOne.Value` | |
| `lineTwo` | `LineTwo.Value` | |
| `cityName` | `CityName.Value` | |
| `countryId` | `CountryID.Value` | |
| `countryName` | `CountryName.Value` | |
| `countrySubDivisionName` | `CountrySubDivisionName.Value` | |

---

### `TradeCountry` ← `SPSCountryType`

| Target field | Source path | Notes |
|---|---|---|
| `id` | `ID.Value` | ISO 3166-1 alpha-2 code e.g. `"GB"` |
| `name` | `Name[].Value` | [language-preferred](#language-selection) |

---

### `Authentication` ← `SPSAuthenticationType`

| Target field | Source path | Notes |
|---|---|---|
| `typeCode` | `TypeCode.Value` | raw code e.g. `"4"` |
| `governmentActionTypeCode` | `TypeCode.name` | human-readable label e.g. `"Inspection"` |
| `actualDateTime` | `ActualDateTime.Item` | ISO 8601 |
| `providerParty` | `ProviderSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `includedClause` | `IncludedSPSClause[]` | see [Clause](#clause--spsclausetype); omitted if empty |

---

### `Clause` ← `SPSClauseType`

| Target field | Source path | Notes |
|---|---|---|
| `identifier` | `ID.Value` | |
| `content` | `Content[].Value` | [neutral-preferred](#language-selection): `null` languageID → context language → first entry |

---

### `IncludedNote` ← `SPSNoteType`

| Target field | Source path | Notes |
|---|---|---|
| `type` | `"Note"` | const default |
| `noteSubjectCode` | `SubjectCode.name ?? SubjectCode.Value` | prefers human-readable name; falls back to code |
| `content` | `Content[lang=en] ?? Content[lang=∅] ?? Content[0]` | prefer English; then language-neutral; then first entry; omitted if no content elements |
| `contentCode` | `ContentCode[]` → `[{ listId, value }]` | one entry per `ContentCode` element; `listID` attribute → `listId`, code value → `value`; omitted if no content code elements |

**Behaviours to be aware of:**

- **Repeated notes with the same `SubjectCode`** — multiple `IncludedSPSNote` elements can share the same `SubjectCode` (e.g. `REFUSAL_REASON`). Each maps to a separate `IncludedNote` object in the JSON array; they are not merged.
- **Multiple `ContentCode` values per note** — a single `IncludedSPSNote` can carry more than one `ContentCode`, each with a distinct `listID` that carries different semantic meaning (e.g. `refusal_reason` vs `refusal_reason_extent`). All are preserved as a list.
- **`Content` alongside or instead of `ContentCode`** — `Content` may appear with no `ContentCode` (e.g. a datetime value) or alongside `ContentCode` (e.g. a free-text establishment name). Both fields are always mapped independently and neither is treated as optional.

---

### `ReferencedDocument` ← `SPSReferencedDocumentType`

| Target field | Source path | Notes |
|---|---|---|
| `typeCode` | `TypeCode.Value` | e.g. `"856"` |
| `relationshipTypeCode` | `RelationshipTypeCode.Value` | e.g. `"CAW"` |
| `identifier` | `ID.Value` | |
| `attachmentBinaryObject` | `null` | binary blobs deferred |
| `information` | `[Information.Value]` | single value wrapped in list; omitted if absent |

---

### `ConsignmentItem` ← `SPSConsignmentItemType`

| Target field | Source path | Notes |
|---|---|---|
| `natureIdCargo` | `NatureIdentificationSPSCargo[]` | see [CargoNature](#cargonature--spsscargotype); omitted if empty |
| `includedTradeLineItem` | `IncludedSPSTradeLineItem[]` | see [TradeLineItem](#tradelineitem--spstradelineitemtype); omitted if empty |

---

### `TradeLineItem` ← `SPSTradeLineItemType`

| Target field | Source path | Notes |
|---|---|---|
| `sequenceNumeric` | `SequenceNumeric.Value` | |
| `description` | `Description[].Value` | [language-preferred list](#language-selection); omitted if none match |
| `scientificName` | `ScientificName[lang=la].Value` | Latin entries only; omitted if none present |
| `netWeight` | `NetWeightMeasure` | see [UneceWeightMeasure](#uneceweightmeasure--measuretype) |
| `grossWeight` | `GrossWeightMeasure` | |
| `applicableProductClassification` | `ApplicableSPSClassification[]` | see [ProductClassification](#productclassification--spsclassificationtype); omitted if empty |
| `physicalReferencedLogisticsPackage` | `PhysicalSPSPackage[]` | see [LogisticsPackage](#logisticspackage--spspackagetype); omitted if empty |

---

### `CargoNature` ← `SPSCargoType`

| Target field | Source path | Notes |
|---|---|---|
| `typeCode` | `TypeCode.Value` | UN/CEFACT cargo type e.g. `"1"` |

---

### `ProductClassification` ← `SPSClassificationType`

| Target field | Source path | Notes |
|---|---|---|
| `systemId` | `SystemID.Value` | classification system identifier e.g. `"CN"` |
| `systemName` | `SystemName[].Value` | [language-preferred](#language-selection) |
| `classCode` | `ClassCode.Value` | e.g. `"0201"` |
| `className` | `ClassName[].Value` | [language-preferred list](#language-selection); omitted if empty |

---

### `LogisticsPackage` ← `SPSPackageType`

| Target field | Source path | Notes |
|---|---|---|
| `levelCode` | `LevelCode.Value` | numeric; omitted if non-numeric |
| `typeCode` | `TypeCode.Value` | UN/CEFACT package type e.g. `"43"` |
| `itemQuantity` | `ItemQuantity.Value` | |

---

### `UneceWeightMeasure` ← `MeasureType`

| Target field | Source path | Notes |
|---|---|---|
| `content` | `Value` | decimal value expressed as string |
| `unitCode` | `unitCode` | UN/CEFACT Rec 20 unit e.g. `"KGM"` |
| `unitCodeListVersionId` | `unitCodeListVersionID` | e.g. `"rec20"` |

---

## Language Selection

The language used for text fields is driven by the `Accept-Language` request header. The primary language tag is extracted (e.g. `en` from `en-GB,en;q=0.9`) and passed as a `MappingContext` through the mapper chain. It also controls the `ISO2AlphaLanguageCode` sent to the TracesNT SOAP service, so the service itself returns text in the requested language where possible.

Most `TextType[]` fields use one of two strategies:

| Strategy | Behaviour | Used by |
|---|---|---|
| **language-preferred** | Context language first; falls back to entries with no `languageID` (`null`); returns `null` if neither found | `name`, `systemName`, `description` list, `className` list |
| **neutral-preferred** | Entries with no `languageID` first (canonical codes); then context language; then first available | `content` on `Clause` |

Some fields use a fixed language regardless of context:

| Field | Fixed language | Reason |
|---|---|---|
| `scientificName` | `la` (Latin) | Scientific names are always Latin |

Fields not backed by `TextType[]` (e.g. addresses, codes, identifiers) are unaffected by language selection.

---

## Date/Time Handling

All date/time fields are output in ISO 8601 format. The source may supply either a structured date-time value or a pre-formatted string; both are handled transparently. A missing date/time maps to `null`.

## Type Code Representation

Type codes are always expressed as their numeric wire values (e.g. `"856"`) rather than descriptive names. Where a human-readable label is also available alongside the code, both are mapped to separate target fields.
