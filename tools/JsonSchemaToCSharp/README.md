# JsonSchemaToCSharp

Generates strongly-typed C# `record` classes from JSON Schema files. Designed for UN/CEFACT UNVTD schemas used in Defra's agricultural compliance system.

## Requirements

- .NET 10 SDK

## Usage

```bash
# Default: reads from ./schema/, writes to ./schema-output/
dotnet run

# Custom arguments
dotnet run -- --schema "/path/to/schemas" --output "/path/to/output" --namespace "My.Namespace"

# Load arguments from a file
dotnet run -- @args.txt
```

### Arguments

| Flag | Description | Default |
|------|-------------|---------|
| `--schema` | Path to directory containing `*.schema.json` files | `./schema/` |
| `--output` | Path to directory where `*.g.cs` files will be written | `./schema-output/` |
| `--namespace` | C# namespace for generated types | `Api.Models.Unece` |

The output directory is created if it does not exist. All `*.g.cs` files in the output directory are deleted before each run.

If no `*.schema.json` files are found, the tool falls back to matching `*.json`.

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
