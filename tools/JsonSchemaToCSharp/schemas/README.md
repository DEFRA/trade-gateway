# Schemas Overview

Input schemas for `JsonSchemaToCSharp`. This tree is a curated subset of
[DEFRA/trade-imports-schemas](https://github.com/DEFRA/trade-imports-schemas) —
only the schemas the gateway consumes are vendored here, not the full published
set. Re-sync by copying individual files, not the whole tree.

Structure and validation follow UNVTD + UN/CEFACT vocabulary typing
(`xsd:string`, `@vocab`, `@id`). JSON-LD contexts and the GBN-AG, PIMS, and
event schemas live upstream and are not needed here.

## Directory map

```text
schemas/
  core/
    defra-unvtd-canonical-core-v1.schema.json            # common building blocks, $ref'd by every profile

  profiles/
    imports/
      international/
        defra-unvtd-profile-ched-v1.schema.json
      eu/
        defra-unvtd-profile-intra-v1.schema.json
        defra-unvtd-profile-docom-v1.schema.json
        defra-unvtd-profile-docom-followup-v1.schema.json  # FollowUpRecord shape + standalone follow-up payload

  reference-data/
    defra-unvtd-profile-reference-data-core-v1.schema.json
    defra-unvtd-profile-reference-data-ClassificationTreeNodeDetailResponse-v1.schema.json
    defra-unvtd-profile-reference-data-ClassificationTreeResponse-v1.schema.json
    defra-unvtd-profile-reference-data-ClassificationSectionListResponse-v1.schema.json
    defra-unvtd-profile-reference-data-MetadataListResponse-v1.schema.json
```

The certificate profiles and the reference-data response schemas are the codegen
entry points; `core` and `reference-data-core` are pulled in transitively via
`$ref`. These roots are declared in `../args.json`.

## Layering

1. `core/defra-unvtd-canonical-core-v1.schema.json` — shared UNVTD-aligned
   building blocks and certificate payload shape.
2. Profile schema under `profiles/imports/...` — type-specific constraints
   (CHED / INTRA / DOCOM document type constraints).

## Regenerating the contract

```sh
cd tools/JsonSchemaToCSharp
dotnet run --project . -- --control-file ./args.json
```

Output lands in `src/Api.Contract/`.

## Migration note

During migration to versioned filenames/paths, transitional internal `$id` and
`$ref` values may still be present. Use file locations in this README as the
source of truth for where artefacts live now.
