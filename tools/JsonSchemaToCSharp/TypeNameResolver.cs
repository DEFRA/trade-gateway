using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;

namespace JsonSchemaToCSharp;

public class TypeNameResolver(SchemaLoader loader, string currentFile)
{
    public string ResolvePropertyTypeName(JsonSchema property)
    {
        return ResolvePropertyTypeName(property, currentFile);
    }

    private string ResolvePropertyTypeName(JsonSchema property, string sourceFile)
    {
        if (ReferenceEquals(property, JsonSchema.False))
            return string.Empty;

        var refKeyword = property.GetKeyword<RefKeyword>();
        if (refKeyword != null)
            return ResolveRefTypeName(refKeyword.Reference.OriginalString, sourceFile);

        var constKeyword = property.GetKeyword<ConstKeyword>();
        if (constKeyword != null)
            return ResolveConstTypeName(constKeyword.Value);

        var enumKeyword = property.GetKeyword<EnumKeyword>();
        if (enumKeyword != null)
            return ResolveEnumTypeName(enumKeyword.Values);

        var typeKeyword = property.GetKeyword<TypeKeyword>();
        if (typeKeyword != null)
        {
            var type = typeKeyword.Type;
            if (type == SchemaValueType.Array)
                return ResolveArrayTypeName(property, sourceFile);
            if (type == SchemaValueType.Object)
                return ResolveObjectTypeName(property);
            if (type == SchemaValueType.String)
                return ResolveStringTypeName(property);
            if (type == SchemaValueType.Integer)
                return DotNetTypes.Int;
            if (type == SchemaValueType.Number)
                return DotNetTypes.Decimal;
            if (type == SchemaValueType.Boolean)
                return DotNetTypes.Bool;
            return DotNetTypes.JsonElement;
        }

        var anyOf = property.GetKeyword<AnyOfKeyword>();
        if (anyOf != null)
            return ResolveAnyOfTypeName(anyOf.Schemas, sourceFile);

        var oneOf = property.GetKeyword<OneOfKeyword>();
        if (oneOf != null)
            return ResolveOneOfTypeName(oneOf.Schemas, sourceFile);

        var allOf = property.GetKeyword<AllOfKeyword>();
        if (allOf != null)
            return ResolveAllOfTypeName(allOf.Schemas, sourceFile);

        return DotNetTypes.Object;
    }

    private string ResolveArrayTypeName(JsonSchema property, string sourceFile)
    {
        var items = property.GetKeyword<ItemsKeyword>()?.SingleSchema;
        if (items == null)
            return $"List<{DotNetTypes.Object}>";

        var itemType = ResolvePropertyTypeName(items, sourceFile);
        return $"List<{itemType}>";
    }

    private string ResolveObjectTypeName(JsonSchema property)
    {
        var refKeyword = property.GetKeyword<RefKeyword>();
        if (refKeyword != null)
        {
            var nameFromRef = ExtractTypeNameFromRef(refKeyword.Reference.OriginalString);
            if (!string.IsNullOrEmpty(nameFromRef))
                return nameFromRef;
        }

        var title = property.GetKeyword<TitleKeyword>()?.Value;
        if (!string.IsNullOrWhiteSpace(title))
            return ToCSharpTypeName(title);

        return DotNetTypes.Object;
    }

    private string ResolveRefTypeName(string refPath, string sourceFile)
    {
        var resolved = loader.Resolve(refPath, sourceFile);
        var target = resolved.Schema;
        var targetFile = resolved.SourceFile;

        var typeKeyword = target.GetKeyword<TypeKeyword>();
        if (typeKeyword != null)
        {
            var type = typeKeyword.Type;
            if (type == SchemaValueType.String)
                return ResolveStringTypeName(target);
            if (type == SchemaValueType.Integer)
                return DotNetTypes.Int;
            if (type == SchemaValueType.Number)
                return DotNetTypes.Decimal;
            if (type == SchemaValueType.Boolean)
                return DotNetTypes.Bool;
            if (type == SchemaValueType.Object)
                return ExtractTypeNameFromRef(refPath) ?? DotNetTypes.Object;
            return DotNetTypes.JsonElement;
        }

        var oneOf = target.GetKeyword<OneOfKeyword>();
        if (oneOf != null)
            return ResolveOneOfTypeName(oneOf.Schemas, targetFile);

        var anyOf = target.GetKeyword<AnyOfKeyword>();
        if (anyOf != null)
            return ResolveAnyOfTypeName(anyOf.Schemas, targetFile);

        var innerRef = target.GetKeyword<RefKeyword>();
        if (innerRef != null)
            return ResolveRefTypeName(innerRef.Reference.OriginalString, targetFile);

        return ExtractTypeNameFromRef(refPath) ?? DotNetTypes.Object;
    }

    private string ResolveAnyOfTypeName(IReadOnlyList<JsonSchema> options, string sourceFile)
    {
        foreach (var option in options)
        {
            var refKeyword = option.GetKeyword<RefKeyword>();
            if (refKeyword != null)
            {
                var type = ResolveRefTypeName(refKeyword.Reference.OriginalString, sourceFile);
                if (type != DotNetTypes.Object)
                    return type;
            }
        }

        foreach (var option in options)
        {
            var constKeyword = option.GetKeyword<ConstKeyword>();
            if (constKeyword != null)
                return ResolveConstTypeName(constKeyword.Value);

            var typeKeyword = option.GetKeyword<TypeKeyword>();
            if (typeKeyword != null)
            {
                var t = typeKeyword.Type;
                if (t != SchemaValueType.Object)
                    return ResolvePropertyTypeName(option, sourceFile);
            }
        }

        return DotNetTypes.Object;
    }

    private string ResolveOneOfTypeName(IReadOnlyList<JsonSchema> options, string sourceFile)
    {
        var hasPrimitive = options.Any(o =>
        {
            var t = o.GetKeyword<TypeKeyword>()?.Type;
            return t
                is SchemaValueType.String
                    or SchemaValueType.Integer
                    or SchemaValueType.Number
                    or SchemaValueType.Boolean;
        });

        var hasRef = options.Any(o => o.GetKeyword<RefKeyword>() != null);
        var hasArray = options.Any(o => o.GetKeyword<TypeKeyword>()?.Type == SchemaValueType.Array);

        // Multiple incompatible JSON value kinds — use JsonElement so any value round-trips
        if (hasPrimitive && (hasRef || hasArray))
            return DotNetTypes.JsonElement;

        foreach (var option in options)
        {
            var refKeyword = option.GetKeyword<RefKeyword>();
            if (refKeyword != null)
                return ResolveRefTypeName(refKeyword.Reference.OriginalString, sourceFile);
        }

        foreach (var option in options)
        {
            var constKeyword = option.GetKeyword<ConstKeyword>();
            if (constKeyword != null)
                return ResolveConstTypeName(constKeyword.Value);

            var typeKeyword = option.GetKeyword<TypeKeyword>();
            if (typeKeyword != null)
            {
                var t = typeKeyword.Type;
                if (t != SchemaValueType.Object)
                    return ResolvePropertyTypeName(option, sourceFile);
            }
        }

        return DotNetTypes.Object;
    }

    private string ResolveAllOfTypeName(IReadOnlyList<JsonSchema> schemas, string sourceFile)
    {
        foreach (var item in schemas)
        {
            var refKeyword = item.GetKeyword<RefKeyword>();
            if (refKeyword != null)
                return ResolveRefTypeName(refKeyword.Reference.OriginalString, sourceFile);

            var oneOf = item.GetKeyword<OneOfKeyword>();
            if (oneOf != null)
                return ResolveOneOfTypeName(oneOf.Schemas, sourceFile);

            var anyOf = item.GetKeyword<AnyOfKeyword>();
            if (anyOf != null)
                return ResolveAnyOfTypeName(anyOf.Schemas, sourceFile);

            var itemsKeyword = item.GetKeyword<ItemsKeyword>()?.SingleSchema;
            if (itemsKeyword != null)
                return $"List<{ResolvePropertyTypeName(itemsKeyword, sourceFile)}>";

            var typeKeyword = item.GetKeyword<TypeKeyword>();
            if (typeKeyword != null)
                return ResolvePropertyTypeName(item, sourceFile);
        }

        return DotNetTypes.Object;
    }

    private static string ResolveStringTypeName(JsonSchema schema)
    {
        var format = schema.GetKeyword<FormatKeyword>()?.Value.Key;
        return format switch
        {
            "date-time" => DotNetTypes.DateTimeOffset,
            "date" => DotNetTypes.DateOnly,
            _ => DotNetTypes.String,
        };
    }

    private static string ResolveEnumTypeName(IReadOnlyList<JsonNode?> values)
    {
        var types = values.Select(NodeType).Distinct().ToList();

        if (types.Count == 1)
        {
            return types[0] switch
            {
                SchemaValueType.String => DotNetTypes.String,
                SchemaValueType.Integer => DotNetTypes.Int,
                SchemaValueType.Number => DotNetTypes.Decimal,
                SchemaValueType.Boolean => DotNetTypes.Bool,
                _ => DotNetTypes.Object,
            };
        }

        return DotNetTypes.Object;
    }

    private static string ResolveConstTypeName(JsonNode? value)
    {
        return NodeType(value) switch
        {
            SchemaValueType.String => DotNetTypes.String,
            SchemaValueType.Integer => DotNetTypes.Int,
            SchemaValueType.Number => DotNetTypes.Decimal,
            SchemaValueType.Boolean => DotNetTypes.Bool,
            _ => DotNetTypes.Object,
        };
    }

    // GetSchemaValueType() from JsonSchema.Net uses GetValue<object>() which throws on .NET 10.
    // This helper uses TryGetValue<T>() instead, which is safe on all runtimes.
    private static SchemaValueType NodeType(JsonNode? node) =>
        node switch
        {
            null => SchemaValueType.Null,
            JsonObject => SchemaValueType.Object,
            JsonArray => SchemaValueType.Array,
            JsonValue v when v.TryGetValue<bool>(out _) => SchemaValueType.Boolean,
            JsonValue v when v.TryGetValue<string>(out _) => SchemaValueType.String,
            JsonValue _ => SchemaValueType.Number,
            _ => SchemaValueType.Null,
        };

    private static string? ExtractTypeNameFromRef(string refPath)
    {
        var pointer = refPath.Contains('#') ? refPath.Split('#', 2)[1] : string.Empty;
        if (string.IsNullOrEmpty(pointer))
            return null;

        var segments = pointer.TrimStart('/').Split('/');
        var lastSegment = segments[^1];

        return DefKeyToCSharpName(lastSegment);
    }

    public static string DefKeyToCSharpName(string defKey)
    {
        var name = defKey;

        if (name.EndsWith("Type", StringComparison.Ordinal))
            name = name[..^4];

        name = Regex.Replace(name, "[^A-Za-z0-9]", string.Empty);

        if (name.Length > 0)
            name = char.ToUpperInvariant(name[0]) + name[1..];

        return string.IsNullOrEmpty(name) ? "Type" : name;
    }

    public static string ToCSharpTypeName(string schemaTitle)
    {
        var sanitized = Regex.Replace(schemaTitle, "[^A-Za-z0-9]+", "_");
        var segments = sanitized.Split(new[] { '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(segments.Select(s => char.ToUpperInvariant(s[0]) + s[1..]));

        if (name.EndsWith("Type", StringComparison.Ordinal))
            name = name[..^4];

        if (name.EndsWith("Details", StringComparison.Ordinal))
            name = name[..^7];

        return string.IsNullOrEmpty(name) ? "SchemaType" : name;
    }

    public static string ToCSharpPropertyName(string jsonName)
    {
        if (string.IsNullOrWhiteSpace(jsonName))
            return "Property";

        var cleaned = jsonName.Trim();
        if (cleaned.StartsWith("$", StringComparison.Ordinal))
            cleaned = cleaned.TrimStart('$');

        cleaned = Regex.Replace(cleaned, "[^A-Za-z0-9]+", "_");
        var segments = cleaned.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var result = string.Concat(segments.Select(ToPascalSegment));

        if (string.IsNullOrEmpty(result))
            result = "Property";

        if (!char.IsLetter(result[0]) && result[0] != '_')
            result = "_" + result;

        return result;
    }

    private static string ToPascalSegment(string s)
    {
        if (s.Length == 0)
            return string.Empty;
        var tail = s.Length > 1 ? s[1..] : string.Empty;
        return char.ToUpperInvariant(s[0]) + tail;
    }
}
