# Reference Data Mappings

This document describes how SOAP types from the TracesNT reference-data service are mapped to the Defra reference-data contract types exposed by `ReferenceDataEndpoints`.

It reflects the current implementation in:

- `src\Api\Endpoints\ReferenceDataEndpoints.cs`
- `src\Api\Mapping\*.cs`
- `src\Api.Contract\ReferenceData\*.g.cs`

---

## Endpoints

### `GET /there` — `ClassificationSectionType[]` → `DefraUNVTDProfileClassificationSectionListResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | set by mapper (`ReferenceDataSource.Traces`) |
| `service` | `"ReferenceDataServiceV1"` | set by mapper (`ReferenceDataService.ReferenceDataServiceV1`) |
| `sections` | `ClassificationSectionType[]` | see [ClassificationSection](#classificationsection--classificationsectiontype) |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `languageCode` argument | primary tag only; defaults to `"en"` |
| none | `ReferenceDataPort.getClassificationSectionsAsync(..., GetClassificationSectionsRequestType)` | request body is empty |

---

### `GET /classificationTrees/{classificationTreeId}` — `ClassificationTreeNode[]` → `DefraUNVTDProfileClassificationTreeResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | set by mapper (`ReferenceDataSource.Traces`) |
| `treeId` | route value `classificationTreeId` | echoed from request, not derived from SOAP payload |
| `nodes` | `ClassificationTreeNode[]` | each node mapped recursively; see [ClassificationTreeNode](#classificationtreenode--classificationtreenode) |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `languageCode` argument | primary tag only; defaults to `"en"` |
| `classificationTreeId` | `GetClassificationTreeRequestType.TreeID` | passed directly from route |

---

### `GET /classificationTrees/{classificationTreeId}/nodes/{nodeId}` — `ClassificationTreeNodeDetail` → `DefraUNVTDProfileClassificationTreeNodeDetailResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | set by mapper |
| `treeId` | route value `classificationTreeId` | echoed from request |
| `nodeId` | route value `nodeId` | route segment used to identify the node |
| `nodePath` |  `ClassificationTreeNodeDetail.path` | Used to also generate nodeId |
| `node` | `ClassificationTreeNodeDetail` | see [Node Detail](#node-detail--classificationtreenodedetail) |
| `attributes` | `ClassificationTreeNodeDetail.Attribute[]` excluding `LegislationNodeAttribute`, `TaxonNodeAttribute`, `ClassificationSectionNodeAttribute`, and `SelectableDocumentLinkNodeAttribute` | see [NodeAttribute](#nodeattribute--abstractnodeattribute) |
| `documentTypes` | `Attribute[]` filtered to `SelectableDocumentLinkNodeAttribute` | see [DocumentNodeAttribute](#documentnodeattribute--selectabledocumentlinknodeattribute) |
| `classificationSectionGroups` | `Attribute[]` filtered to `ClassificationSectionNodeAttribute` | see [ClassificationSectionGroup](#classificationsectiongroup--classificationsectionnodeattribute) |
| `legislationAttributes` | `Attribute[]` filtered to `LegislationNodeAttribute` | see [LegislationAttribute](#legislationattribute--legislationnodeattribute) |
| `taxons` | `Attribute[]` select `TaxonNodeAttribute` where `id` is `TAXON_POSSIBLE_VALUES` | exact id match; any other taxon attribute ids are ignored |
| `invasiveTaxons` | `Attribute[]` select `TaxonNodeAttribute` where `id` is `INVASIVE_TAXON_POSSIBLE_VALUES` | exact id match; kept separate to avoid data loss |
| `retrievedAt` | `DateTimeOffset.UtcNow` | API generation timestamp |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `languageCode` argument | primary tag only; defaults to `"en"` |
| `classificationTreeId` | `GetClassificationTreeNodeDetailRequestType.TreeID` | passed directly from route |
| `path` | `GetClassificationTreeNodeDetailRequestType.Item` as `string` | the node path |


**Behaviours to be aware of**

- Either `path` or `cnCode` is required; the endpoint returns `400 Bad Request` if both are blank.
- The SOAP request uses a polymorphic `Item` field: `path` is sent as a raw string, while `cnCode` is sent as a `CodeType`.
- Some SOAP attribute subtypes are **not** duplicated in generic `attributes`; they are emitted through dedicated fields:
  - `LegislationNodeAttribute` → `legislationAttributes`
  - `TaxonNodeAttribute` (id `TAXON_POSSIBLE_VALUES` / `INVASIVE_TAXON_POSSIBLE_VALUES`) → `taxons` / `invasiveTaxons`
  - `ClassificationSectionNodeAttribute` → `classificationSectionGroups`
  - `SelectableDocumentLinkNodeAttribute` → `documentTypes`

---

### `GET /metaDatas/{metadataType}` — `MetadataCodeType[]` → `DefraUNVTDProfileMetadataListResponse`

| Target field | Source path | Notes |
|---|---|---|
| `source` | `"traces"` | set by mapper |
| `metadataType` | route value `metadataType` | echoed from request |
| `items` | `MetadataCodeType[]` | see [MetadataCode](#metadatacode--metadatacodetype) |
| `retrievedAt` | `DateTime.UtcNow` | assigned in mapper; serialized as `DateTimeOffset?` by the contract |

**SOAP call**

| Input | SOAP request path | Notes |
|---|---|---|
| `Accept-Language` | `AcceptLanguageParser.GetPrimaryLanguageCode(...)` → SOAP `languageCode` argument | primary tag only; defaults to `"en"` |
| `metadataType` | `GetMetadatasRequestType.MetadataType` | passed directly from route |

---

## Shared Types

### `ClassificationSection` ← `ClassificationSectionType`

| Target field | Source path | Notes |
|---|---|---|
| `classCode` | `code` | required |
| `chapter` | `ClassificationSectionChapter?.Value` | omitted if missing |
| `lms` | `lms` | required |
| `description` | `Description.Value` | required |
| `active` | `active` | |
| `scopes` | `MetaCountryGroupScope[].Value` | filtered to non-empty values; empty list if none |
| `operatorActivities` | `OperatorActivityType[]` | `.Value` used to hold the activity type; filtered to non-empty values; empty list if none |

---

### `ClassificationSection` ← `ClassificationSectionReference`

| Target field | Source path | Notes |
|---|---|---|
| `classCode` | `code` | required |
| `chapter` | `chapter` | |
| `lms` | `lms` | required |
| `description` | `Description.Value` | required |
| `active` | not populated | contract field exists, reference mapper does not currently set it |
| `scopes` | `Scope[].Value` | filtered to non-empty values; empty list if none |
| `operatorActivities` | `OperatorActivityType[]` | `.type` used to hold the activity type; filtered to non-empty values; empty list if none |

---

### `ClassificationTreeNode` ← `ClassificationTreeNode`

| Target field | Source path | Notes |
|---|---|---|
| `nodeId` | `nodeId` | required |
| `path` | `path` | required |
| `label` | `Description.Value` | required |
| `nodeType` | `type` | mapped by [Node Type Mapping](#node-type-mapping) |
| `selectable` | `allowedForSelection` | required |
| `cnCode` | `Item.Value` when `Item is CodeType` | omitted for non-CN nodes |
| `children` | `Node[]` | recursively mapped; omitted if empty |
| `certificate` | `Item` | mapped to a `CertificateModelReference` object (intra only); omitted otherwise |

---

### Node Detail ← `ClassificationTreeNodeDetail`

| Target field | Source path | Notes |
|---|---|---|
| `cnCode` | `(Item as CodeType)?.Value` | populated for CN/nomenclature nodes |
| `certificateModel` | `Item` when `Item is CertificateModelReference` | mapped to a `CertificateModelReference` object; omitted otherwise |
| `certificateModel.modelId` | `Item.modelId` | cast from `long` to `int` |
| `certificateModel.shortTitle` | `Item.ShortTitle.Value` | |
| `certificateModel.longTitle` | `Item.LongTitle.Value` | |
| `certificateModel.createdOn` | `Item.createdOn` | converted to UTC |
| `certificateModel.updatedOn` | `Item.updatedOn` when `updatedOnSpecified` | converted to UTC; omitted otherwise |
| `selectable` | `allowedForSelection` | required |
| `nodeType` | `type` | mapped by [Node Type Mapping](#node-type-mapping) |
| `label` | `Description.Value` | used for both nomenclature and non-CN nodes |

---

### `NodeAttribute` ← `AbstractNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `key` | `id` | no fallback to `mappedId` or CLR type name in the current mapper |
| `description` | `Description.Value` | |
| `value` | attribute-type specific | see [Attribute Value Shapes](#attribute-value-shapes) |

`LegislationNodeAttribute`, `TaxonNodeAttribute`, `ClassificationSectionNodeAttribute`, and `SelectableDocumentLinkNodeAttribute` are **not** mapped with this generic mapper (calling `NodeAttributeMapper` for these types throws); they are handled by dedicated mappers and surfaced via `legislationAttributes`, `taxons`/`invasiveTaxons`, `classificationSectionGroups`, and `documentTypes` respectively.

---

### `ClassificationSectionGroup` ← `ClassificationSectionNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `id` | `id` | attribute id is used as the group id |
| `description` | `Description.Value` | required |
| `sections[]` | `ClassificationSection[]` | each item mapped; see [ClassificationSection](#classificationsection--classificationsectionreference) |

---

### `DocumentNodeAttribute` ← `SelectableDocumentLinkNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `key` | `id` | |
| `description` | `Description.Value` | |
| `documentLinkTypes[]` | `DocumentTypeValue[]` | mapped to `DocumentNodeAttributeValue[]` |

---

### `DocumentNodeAttributeValue` ← `SelectableDocumentLinkNodeAttributeValue`

| Target field | Source path | Notes |
|---|---|---|
| `documentType` | `Value` | |
| `linkType` | `linkType` | |

---

### `LegislationAttribute` ← `LegislationNodeAttribute`

| Target field | Source path | Notes |
|---|---|---|
| `key` | `id` | |
| `description` | `Description.Value` | |
| `legislation` | `LegislationReference` | mapped as a single object (not a list) |
| `legislation.legislationId` | `LegislationReference.legislationId` | cast from `long` to `int` |
| `legislation.celexIdentifiers` | `LegislationReference.CelexIdentifier[].Value` | filtered to non-empty values |
| `legislation.certificateModels[]` | `LegislationReference.CertificateModel[]` | omitted if empty |
| `legislation.certificateModels[].modelId` | `LegislationReference.CertificateModel[].modelId` | cast from `long` to `int` |
| `legislation.certificateModels[].shortTitle` | `LegislationReference.CertificateModel[].ShortTitle?.Value` | |
| `legislation.certificateModels[].longTitle` | `LegislationReference.CertificateModel[].LongTitle?.Value` | |
| `legislation.certificateModels[].createdOn` | `LegislationReference.CertificateModel[].createdOn` | converted to UTC |
| `legislation.certificateModels[].updatedOn` | `LegislationReference.CertificateModel[].updatedOn` when `updatedOnSpecified` | converted to UTC; omitted otherwise |
| `legislation.originCountries` | `LegislationReference.OriginCountry[].Value` | filtered to non-empty values |
| `legislation.destinationCountries` | `LegislationReference.DestinationCountry[].Value` | filtered to non-empty values |
| `legislation.originClassificationSections` | `LegislationReference.OriginClassificationSection[]` | see [ClassificationSection](#classificationsection--classificationsectionreference) |
| `legislation.destinationClassificationSections` | `LegislationReference.DestinationClassificationSection[]` | see [ClassificationSection](#classificationsection--classificationsectionreference) |

---

### `Taxon` ← `TaxonReference`

| Target field | Source path | Notes |
|---|---|---|
| `taxonId` | `taxonId` | cast from `long` to `int` |
| `eppoCode` | `eppoCode` | |
| `faoCode` | `faoCode` | |
| `name` | `Value` | copied directly; no fallback to `taxonId` in the current mapper |
| `languageId` | `languageID` | copied directly from TracesNT text metadata |

---

### `MetadataCode` ← `MetadataCodeType`

| Target field | Source path | Notes |
|---|---|---|
| `value` | `Value` | required |
| `mappedValue` | `mappedValue` | |
| `active` | `active` | |
| `displayName` | `name` | |

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
| `DescriptorColumnNodeAttribute` | string array | `DescriptorColumnValue[].id` |
| any other subtype | omitted | mapper returns `null` |

All string arrays are filtered to remove null, empty, and whitespace-only values. If the resulting array is empty, `value` is omitted.

Note: `LegislationNodeAttribute`, `TaxonNodeAttribute`, `ClassificationSectionNodeAttribute`, and `SelectableDocumentLinkNodeAttribute` are not mapped through generic `NodeAttribute.value` in the node-detail response; they are handled separately via `legislationAttributes`, `taxons`/`invasiveTaxons`, `classificationSectionGroups`, and `documentTypes`.

---

## Node Type Mapping

| TracesNT `ClassificationTreeNodeType` | API `nodeType` |
|---|---|
| `nomenclature` | `nomenclature` |
| `label` | `label` |
| `taxon` | `group` |
| `certificate_model` | `certificate` |
| `no_commodity` | `other` |

---

## Service Behaviour

Reference-data endpoints accept `Accept-Language`. The header is reduced to its primary language tag by `AcceptLanguageParser`:

- `en-GB,en;q=0.9` -> `en`
- `cy-GB,cy;q=0.9,en;q=0.8` -> `cy`
- missing or blank header -> `en`

That primary language code is converted to TracesNT `ISO2AlphaLanguageCodeContentType` and sent to the SOAP reference-data service, so labels and descriptions are requested in the caller's preferred language where Traces provides them.
