# Certificate Mappings

This document describes how SOAP types from the TracesNT service are mapped to the Defra contract types. Shared type mappings are reused across all certificate types.

---

## Certificates

### Intra — `EuIntraCertificateType` → `DefraUNVTDINTRAProfile`

| Target field | Source path | Notes |
|---|---|---|
| `$model` | `"defra/certificate-internal/1"` | const default |
| `$type` | `"intra"` | const default |
| `exchangedDocument.documentTypeCode` | `SPSCertificate.SPSExchangedDocument.TypeCode.Value` | profile-specific type `DefraUNVTDINTRAProfileExchangedDocument`; only `documentTypeCode` is mapped |
| `specifiedConsignment[0]` | `SPSCertificate.SPSConsignment` | single-item list; see [Consignment](#consignment--spsconsignmenttype) |
| `laboratoryObservationResult` | `null` | `SPSConsignmentItemLaboratoryTest` not mapped in v1 |

---

### CHED — `ChedCertificateType` → `DefraUNVTDCHEDProfile`

| Target field | Source path | Notes |
|---|---|---|
| `$model` | `"defra/certificate-internal/1"` | const default |
| `$type` | `"ched"` | const default |
| `exchangedDocument.documentTypeCode` | `SPSCertificate.SPSExchangedDocument.TypeCode.Value` | profile-specific const `636` applied via profile (shared ExchangedDocument type) |
| `specifiedConsignment` | `SPSCertificate.SPSConsignment` | maps to a see [Consignment](#consignment--spsconsignmenttype) object |
| `laboratoryObservationResult` | `null` | `SPSConsignmentItemLaboratoryTest` not mapped in v1 |

---

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
| `issuer` | `IssuerSPSParty` | see [TradeParty](#tradeparty--spspartytype); the party responsible for issuing the document |
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
| `deliveryParty` | `DeliverySPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `carrier` | `CarrierSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `customsTransitAgentParty` | `CustomsTransitAgentSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
| `exportCountry` | `ExportSPSCountry` | see [TradeCountry](#tradecountry--spscountrytype) |
| `originCountry` | `OriginSPSCountry` | see [TradeCountry](#tradecountry--spscountrytype) |
| `importCountry` | `ImportSPSCountry` | see [TradeCountry](#tradecountry--spscountrytype) |
| `reExportCountry` | `ReExportSPSCountry[]` | list; omitted if empty, see [TradeCountry](#tradecountry--spscountrytype) |
| `transitCountry` | `TransitSPSCountry[]` | list; omitted if empty, see [TradeCountry](#tradecountry--spscountrytype) |
| `transitTradeCountry` | `TransitSPSCountry[]` | see [TradeCountry](#tradecountry--spscountrytype) |
| `unloadingBaseportLocation` | `UnloadingBaseportSPSLocation` | see [LogisticsLocation](#logisticslocation--spslocationtype) |
| `mainCarriageLogisticsTransportMovement` | `MainCarriageSPSTransportMovement[]` | list, one entry per carriage leg; see [LogisticsTransportMovement](#logisticstransportmovement--spstransportmovementtype); omitted if empty |
| `packageQuantity` | `— (no direct SOAP equivalent on SPSConsignmentType)` | The canonical `packageQuantity` slot exists on the contract type but is not present on all SOAP variants; it remains unmapped unless a source element is available in the SOAP payload |
| `includedConsignmentItem` | `IncludedSPSConsignmentItem[]` | see [ConsignmentItem](#consignmentitem--spsconsignmentitemtype); omitted if empty |

---

### `TradeParty` ← `SPSPartyType`

| Target field | Source path | Notes |
|---|---|---|
| `identifier` | `ID.Value` | |
| `name` | `Name.Value` | |
| `partyRoleCode` | `RoleCode` | [coded value](#coded-values): `value` ← `RoleCode.Value`, `name` ← `RoleCode.name`, `urlId` from `RoleCode.listID`; omitted if absent |
| `partyTypeCode` | `TypeCode[]` | list of [coded values](#coded-values); a party can carry codes from more than one TRACES list, all are mapped (entries with no value are skipped); omitted if empty |
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
| `code` | `ID` / `Name` | [coded value](#coded-values): `value` ← `ID.Value` (ISO 3166-1 alpha-2 e.g. `"GB"`), `name` ← `Name[].Value` [language-preferred](#language-selection) |

---

### `LogisticsLocation` ← `SPSLocationType`

| Target field | Source path | Notes |
|---|---|---|
| `identifier` | `ID.Value` | bare location identifier e.g. `"GBDVR1"` |
| `urlId` | `ID.schemeID` | codelist/register the identifier is drawn from (e.g. `un_locode`), expressed as a `https://traces-codelists.ec.europa.eu/{schemeID}` URI; omitted if no scheme |
| `name` | `Name[].Value` | [language-preferred](#language-selection) |
| `typeCode` | `null` | no SOAP source on `SPSLocationType` |
| `postalAddress` | `null` | no SOAP source on `SPSLocationType` |

`SPSLocationType` carries only `ID` and `Name`, so `typeCode` and `postalAddress` are unmapped.

---

### `Authentication` ← `SPSAuthenticationType`

| Target field | Source path | Notes |
|---|---|---|
| `typeCode` | `TypeCode.Value` | raw code e.g. `"4"` |
| `governmentActionTypeCode` | `TypeCode.name` | human-readable label e.g. `"Inspection"` |
| `actualDateTime` | `ActualDateTime.Item` | ISO 8601 |
| `provider` | `ProviderSPSParty` | see [TradeParty](#tradeparty--spspartytype) |
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
| `subject` | `SubjectCode.Value` | raw subject code e.g. `"REFUSAL_REASON"` |
| `content` | `Content[].Value` | every content element, in source order; omitted if no content elements |
| `contentCode` | `ContentCode[]` | list of [coded values](#coded-values): `value` ← code value, `urlId` ← `listID` attribute; one entry per `ContentCode` element; omitted if empty |

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
| `scientificName` | `ScientificName.Value` | [language-preferred](#language-selection) with fixed Latin (`la`); omitted if none present |
| `netWeight` | `NetWeightMeasure` | see [UneceWeightMeasure](#uneceweightmeasure--measuretype) |
| `grossWeight` | `GrossWeightMeasure` | |
| `applicableClassification` | `ApplicableSPSClassification[]` | see [ApplicableClassification](#applicableclassification--spsclassificationtype); omitted if empty |
| `physicalReferencedLogisticsPackage` | `PhysicalSPSPackage[]` | see [LogisticsPackage](#logisticspackage--spspackagetype); omitted if empty |

---

### `LogisticsTransportMovement` ← `SPSTransportMovementType`

| Target field | Source path | Notes |
|---|---|---|
| `identifier` | `ID.Value` | transport identifier (vessel name, flight number, vehicle registration) |
| `modeCode` | `ModeCode.Value` | UN/EDIFACT Rec 19 wire code e.g. `"3"` (Road); from the `[XmlEnum]` value |
| `usedLogisticsTransportMeans.name` | `UsedSPSTransportMeans.Name.Value` | omitted if no transport-means name |
| `urlId` | `null` | no SOAP source |
| `transportContractRelatedReferencedDocument` | `null` | no SOAP source |
| `arrivalEvent` | `null` | no SOAP source |
| `departureEvent` | `null` | no SOAP source |

`SPSConsignment.MainCarriageSPSTransportMovement` is a SOAP array and the contract slot is now a
list (the SPS profile collapses BSP's pre/main/on-carriage split into this single slot, with one
entry per carriage leg). Every element is mapped; an empty or absent array maps to `null`.

---

### `CargoNature` ← `SPSCargoType`

| Target field | Source path | Notes |
|---|---|---|
| `typeCode` | `TypeCode.Value` | UN/CEFACT cargo type e.g. `"1"` |

---

### `ApplicableClassification` ← `SPSClassificationType`

| Target field | Source path | Notes |
|---|---|---|
| `systemId` | `SystemID.Value` | classification system identifier e.g. `"CN"` |
| `systemName` | `SystemName[].Value` | [language-preferred](#language-selection) |
| `classCode` | `ClassCode.Value` | [coded value](#coded-values): `value` ← code e.g. `"0201"`, `urlId` built from the `class_code_system` codelist keyed by `SystemID.Value`; omitted if no code |
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

## Coded Values

Several fields are mapped to a shared `CodedValue` shape rather than a bare string:

| Field | Source | Notes |
|---|---|---|
| `value` | the code itself | required |
| `name` | the SOAP `name` attribute / language-selected label | human-readable label; omitted if absent |
| `urlId` | the codelist the value is drawn from | a `https://traces-codelists.ec.europa.eu/{listId}` URI; `listId` comes from the source `listID` attribute (e.g. `"3035"` for party role codes) or a fixed codelist name (e.g. `class_code_system`); omitted when no codelist is known |

Fields using this shape: `TradeCountry.code`, `TradeParty.partyRoleCode`, `TradeParty.partyTypeCode[]`, `ApplicableClassification.classCode`, and `IncludedNote.contentCode[]`.
