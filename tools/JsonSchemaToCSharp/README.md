# JsonSchemaToCSharp

Generates strongly-typed C# `record` classes from JSON Schema files. Designed for UN/CEFACT UNVTD schemas used in Defra's agricultural compliance system.

## Requirements

- .NET 10 SDK

## Usage

This tool is driven by a control JSON file that specifies which schema files to process, the output root, and namespace mappings.

```bash
# Run with a control file (required)
dotnet run -- --control-file "./args.json"
```

The control file must be a JSON object with these required properties:

- `outputRoot` (string): path where generated `*.g.cs` files will be written (relative paths are resolved from the repository root when running via dotnet run)
- `namespaceRoot` (string): root C# namespace to prepend to group namespaces
- `schemas` (array): list of schema groups; each group must contain:
  - `namespace` (string)
  - `schemaItems` (array of strings): paths to individual schema files

Example control file:

```json
{
  "outputRoot": "./../../src/Api.Contract",
  "namespaceRoot": "Api.Models",
  "schemas": [
    {
      "namespace": "Certificate",
      "schemaItems": [
        "schemas/profiles/imports/international/defra-unvtd-profile-ched-v1.schema.json",
        "schemas/profiles/imports/eu/defra-unvtd-profile-intra-v1.schema.json"
      ]
    },
    {
      "namespace": "ReferenceData",
      "schemaItems": [
        "schemas/reference-data/defra-unvtd-profile-reference-data-ClassificationSectionListResponse-v1.schema.json",
        "schemas/reference-data/defra-unvtd-profile-reference-data-ClassificationTreeNodeDetailResponse-v1.schema.json",
        "schemas/reference-data/defra-unvtd-profile-reference-data-ClassificationTreeResponse-v1.schema.json",
        "schemas/reference-data/defra-unvtd-profile-reference-data-MetadataListResponse-v1.schema.json"
      ]
    }
  ]
}
```

The output directory is created if it does not exist. All `*.g.cs` files in the output directories are deleted before each run.

Control file validation is performed; missing required fields cause the tool to exit with an error message. If you want the older behavior of globbing a schema folder, create a control file that lists the schema files to process.

## Output

Each type defined in a schema's `$defs` that has `"type": "object"` and at least one property generates one `<TypeName>.g.cs` file. Root-level object schemas and `allOf` compositions also generate a file named after the schema's `title` or filename.

Generated files follow this pattern:

```csharp
#nullable enable
namespace Api.Models.Unece;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

public partial record ConsignmentItem
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("description")]
    [Description("A textual description of the consignment item.")]
    public string? Description { get; init; }

    [JsonPropertyName("associatedTransportEquipment")]
    public List<TransportEquipment>? AssociatedTransportEquipment { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
```

Key properties of the generated code:

- All records are `partial` — extend them in separate non-`.g.cs` files without modifying generated output
- Properties required by the JSON Schema use the `required` keyword; optional properties are nullable
- `[JsonPropertyName]` preserves the original JSON field name
- `[Description]` is emitted when the schema includes a `description` for the property
- `ExtensionData` captures any unknown JSON fields for round-trip fidelity

## Type mapping

| JSON Schema type | C# type |
|-----------------|---------|
| `string` | `string` |
| `integer` | `int` |
| `number` | `decimal` |
| `boolean` | `bool` |
| `array` | `List<T>` |
| `object` with properties | Generated record type |
| `object` inline (no `$ref`) | Nested record named `ParentProperty` |
| `$ref` to object type | Referenced record type by name |
| `enum` / `const` | Inferred scalar type |
| Mixed `oneOf`/`anyOf` (primitive + ref) | `JsonElement` |

`$ref` pointers are resolved across files using RFC 6901 JSON Pointer syntax (e.g. `other-file.schema.json#/$defs/FooType`).

## Skipped types

- `pdt`, `udt`, and `qdt` categories (primitive/unqualified/qualified data types — these are referenced as scalars rather than expanded)
- `$defs` entries that are not `"type": "object"` or have no `properties`

## Tests

```bash
# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~RoundTrip_OptionAIntraExample_PreservesAllData"
```

Tests in [Tests/RoundTripTests.cs](Tests/RoundTripTests.cs) deserialize the sample document [schema/option-a-unvtd-intra.json](schema/option-a-unvtd-intra.json), re-serialize it, and deep-compare the JSON trees to confirm no data loss.
