# Reference Data Mappings

This document describes how SOAP types from the TracesNT reference-data service are mapped to the Defra reference-data contract types exposed by `ReferenceDataEndpoints`.

---

## Endpoints

### `GET /classificationSections` — `GetClassificationSectionsResponse` → `DefraUNVTDProfileClassificationSectionListResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | const default |
| `service` | `"ReferenceDataServiceV1"` | const default |
| `treeId` | `"intra_trade"` | const default |
| `sections` | `GetClassificationSectionsResponse1[]` | see [ClassificationSection](#classificationsection--classificationsectiontype) |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `languageCode` header → `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `LanguageCode` argument | primary tag only; defaults to `"en"` |
| none | `ReferenceDataPort.getClassificationSectionsAsync(..., GetClassificationSectionsRequestType)` | request body is empty |

---

### `GET /classificationTrees/{classificationTreeId}` — `GetClassificationTreeResponse` → `DefraUNVTDProfileClassificationTreeResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | const default |
| `treeId` | route value `classificationTreeId` | echoed from request, not derived from SOAP payload |
| `root` | `GetClassificationTreeResponse1[0]` | first returned root node; see [ClassificationTreeNode](#classificationtreenode--classificationtreenode) |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `languageCode` header → `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `LanguageCode` argument | primary tag only; defaults to `"en"` |
| `classificationTreeId` | `GetClassificationTreeRequestType.TreeID` | passed directly from route |

---

### `GET /classificationTrees/{classificationTreeId}/nodedetail` — `GetClassificationTreeNodeDetailResponse` → `DefraUNVTDProfileClassificationTreeNodeDetailResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | const default |
| `treeId` | route value `classificationTreeId` | echoed from request |
| `nodePath` | `GetClassificationTreeNodeDetailResponse1.Node.path` | |
| `node` | `GetClassificationTreeNodeDetailResponse1.Node` | see [Node Detail](#node-detail--classificationtreenodedetail) |
| `attributes` | `GetClassificationTreeNodeDetailResponse1.Node.Attribute[]` | see [NodeAttribute](#nodeattribute--abstractnodeattribute) |
| `classificationSections` | `Node.Attribute[]` filtered to `ClassificationSectionNodeAttribute`, then flattened from `ClassificationSection[]` | see [ClassificationSection](#classificationsection--classificationsectionreference) |
| `legislationAttributes` | `Node.Attribute[]` filtered to `LegislationNodeAttribute` | see [LegislationAttribute](#legislationattribute--legislationnodeattribute) |
| `taxons` | `Node.Attribute[]` filtered to `TaxonNodeAttribute`, then flattened from `TaxonReference[]` | see [Taxon](#taxon--taxonreference) |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `languageCode` header → `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `LanguageCode` argument | primary tag only; defaults to `"en"` |
| `classificationTreeId` | `GetClassificationTreeNodeDetailRequestType.TreeID` | passed directly from route |
| `path` | `GetClassificationTreeNodeDetailRequestType.Item` as `string` | used when `path` query string is supplied |
| `cnCode` | `GetClassificationTreeNodeDetailRequestType.Item.Value` in a `CodeType` | used when `cnCode` query string is supplied |

**Behaviours to be aware of:**

- **Either `path` or `cnCode` is required** — the endpoint returns `400 Bad Request` before calling Traces when both are missing.
- **The SOAP request uses a polymorphic `Item` field** — `path` is sent as a raw string, while `cnCode` is sent as a `CodeType`.
---

## Shared Types

### `ClassificationSection` ← `ClassificationSectionType`

| Target field | Source path | Notes |
|---|---|---|
| `classCode` | `code` | required attribute |
| `chapter` | `ClassificationSectionChapter.Value` | omitted if missing |
| `lms` | `lms` | |
| `description` | `Description.Value` | required |
| `active` | `active` | |
| `scopes` | `MetaCountryGroupScope[].Value` | filtered to non-empty values; empty list if none |

---

### `ClassificationSection` ← `ClassificationSectionReference`

| Target field | Source path | Notes |
|---|---|---|
| `classCode` | `code` | required attribute |
| `chapter` | `chapter` | |
| `lms` | `lms` | |
| `description` | `Description.Value` | required |
| `scopes` | `Scope[].Value` | filtered to non-empty values; empty list if none |

---

### `ClassificationTreeNode` ← `ClassificationTreeNode`

| Target field | Source path | Notes |
|---|---|---|
| `path` | `path` | required attribute |
| `label` | `Description.Value` | required |
| `nodeType` | `type` | mapped by [Node Type Mapping](#node-type-mapping) |
| `selectable` | `allowedForSelection` | |
| `cnCode` | `Item.Value` when `Item is CodeType` | omitted for non-CN nodes |
| `children` | `Node[]` | recursively mapped; omitted if empty |

---

### Node Detail ← `ClassificationTreeNodeDetail`

| Target field | Source path | Notes |
|---|---|---|
| `cnCode` | `Item.Value` when `Item is CodeType` | |
| `modelId` | `Item.modelId` when `Item is CertificateModelReference` | emitted as string |
| `selectable` | `allowedForSelection` | |
| `nodeType` | `type` | mapped by [Node Type Mapping](#node-type-mapping) |
| `label` | `Description.Value` | |

---

### `NodeAttribute` ← `AbstractNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `key` | `id ?? mappedId ?? CLR type name` | fallback order used by mapper |
| `description` | `Description.Value` | |
| `value` | attribute-type specific | see [Attribute Value Shapes](#attribute-value-shapes) |

---

### `LegislationAttribute` ← `LegislationNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `key` | `id` | |
| `description` | `Description.Value` | |
| `legislation[].legislationId` | `LegislationReference.legislationId` | cast from `long` to `int` |
| `legislation[].celexIdentifiers` | `LegislationReference.CelexIdentifier[].Value` | filtered to non-empty values |
| `legislation[].certificateModels[].modelId` | `LegislationReference.CertificateModel[].modelId` | cast from `long` to `int` |
| `legislation[].certificateModels[].shortTitle` | `LegislationReference.CertificateModel[].ShortTitle.Value` | |
| `legislation[].certificateModels[].longTitle` | `LegislationReference.CertificateModel[].LongTitle.Value` | |
| `legislation[].certificateModels[].createdOn` | `LegislationReference.CertificateModel[].createdOn` | |
| `legislation[].certificateModels[].updatedOn` | `LegislationReference.CertificateModel[].updatedOn` when `updatedOnSpecified` | omitted otherwise |
| `legislation[].originCountries` | `LegislationReference.OriginCountry[].Value` | filtered to non-empty values |
| `legislation[].destinationCountries` | `LegislationReference.DestinationCountry[].Value` | filtered to non-empty values |
| `legislation[].originClassificationSections` | `LegislationReference.OriginClassificationSection[]` | see [ClassificationSection](#classificationsection--classificationsectionreference) |
| `legislation[].destinationClassificationSections` | `LegislationReference.DestinationClassificationSection[]` | see [ClassificationSection](#classificationsection--classificationsectionreference) |

---

### `Taxon` ← `TaxonReference`

| Target field | Source path | Notes |
|---|---|---|
| `taxonId` | `taxonId` | cast from `long` to `int` |
| `eppoCode` | `eppoCode` | |
| `faoCode` | `faoCode` | |
| `name` | `Value` | falls back to `taxonId.ToString()` if empty |
| `languageId` | `languageID` | copied directly from TracesNT text metadata |

---

## Attribute Value Shapes

The `NodeAttribute.value` field is serialized according to the concrete TracesNT attribute subtype:

| SOAP type | JSON value shape | Source path |
|---|---|---|
| `BooleanNodeAttribute` | boolean | `BooleanValue` |
| `IntegerNodeAttribute` | integer if parseable, otherwise string | `IntegerValue` |
| `IntegerRangeNodeAttribute` | string array | `[Min, Max]` |
| `DoubleRangeNodeAttribute` | string array | `Min` / `Max` using invariant culture, only when `MinSpecified` / `MaxSpecified` |
| `EnumSingleNodeAttribute` | string | `EnumValue.Value` |
| `EnumCollectionNodeAttribute` | string array | `EnumValue[].Value` |
| `FieldAccessNodeAttribute` | string | `FieldAccessValue.ToString()` |
| `MandatoryNotApplicableNodeAttribute` | string | `MandatoryNotApplicableValue.ToString()` |
| `CardinalityNodeAttribute` | string | `CardinalityValue.ToString()` |
| `AllowedNodeAttribute` | string | `AllowedValue.ToString()` |
| `ClassificationSectionNodeAttribute` | string array | `ClassificationSection[].code` |
| `TaxonNodeAttribute` | string array | `TaxonReference[].Value` |
| `LegislationNodeAttribute` | string array | `LegislationReference.CelexIdentifier[].Value` |
| `SelectableDocumentLinkNodeAttribute` | string array | `DocumentTypeValue[].Value` |
| `DescriptorColumnNodeAttribute` | string array | `DescriptorColumnValue[].id` |

All string arrays are filtered to remove null, empty, and whitespace-only values. If the resulting array is empty, `value` is omitted.

---

## Node Type Mapping

| TracesNT `ClassificationTreeNodeType` | API `nodeType` |
|---|---|
| `nomenclature` | `nomenclature` |
| `label` | `label` |
| `taxon` | `group` |
| `certificate_model` | `other` |
| `no_commodity` | `other` |

---

## Service Behaviour

Reference-data endpoints now accept `Accept-Language`. The header is reduced to its primary language tag by `AcceptLanguageParser`:

- `en-GB,en;q=0.9` → `en`
- `cy-GB,cy;q=0.9,en;q=0.8` → `cy`
- missing or blank header → `en`

That primary language code is converted to TracesNT `ISO2AlphaLanguageCodeContentType` and sent to the SOAP reference-data service, so labels and descriptions are requested in the caller's preferred language where Traces provides them.
