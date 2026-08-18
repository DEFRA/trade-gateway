using Json.Schema;

namespace JsonSchemaToCSharp;

public record ResolvedReference(JsonSchema Schema, string SourceFile);

public class SchemaLoader(string basePath)
{
    private readonly string _basePath = Path.GetFullPath(basePath);
    private readonly Dictionary<string, JsonSchema> _cache = new();

    public JsonSchema LoadFile(string path)
    {
        var fullPath = NormalizePath(path);

        if (_cache.TryGetValue(fullPath, out var cached))
            return cached;

        var json = File.ReadAllText(fullPath);
        var schema = JsonSchema.FromText(json);
        _cache[fullPath] = schema;
        return schema;
    }

    public ResolvedReference Resolve(string refPath, string currentFile)
    {
        string fileName;
        string referenceName;

        if (refPath.Contains('#'))
        {
            var parts = refPath.Split('#', 2);
            fileName = parts[0];
            referenceName = parts[1];
        }
        else
        {
            fileName = refPath;
            referenceName = "";
        }

        string resolvedPath;
        if (string.IsNullOrEmpty(fileName))
        {
            resolvedPath = NormalizePath(currentFile);
        }
        else
        {
            var currentDir = Path.GetDirectoryName(currentFile) ?? string.Empty;
            resolvedPath = NormalizePath(Path.Combine(currentDir, fileName));
        }

        var jsonSchemaRoot = LoadFile(resolvedPath);

        if (string.IsNullOrEmpty(referenceName) || referenceName == "/")
            return new ResolvedReference(jsonSchemaRoot, resolvedPath);

        return new ResolvedReference(ResolveReference(jsonSchemaRoot, referenceName), resolvedPath);
    }

    private string NormalizePath(string path)
    {
        var candidate = path.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(_basePath, candidate));
    }

    private static JsonSchema ResolveReference(JsonSchema schema, string reference)
    {
        var segments = reference.TrimStart('/').Split('/');
        var current = schema;

        var i = 0;
        while (i < segments.Length)
        {
            var segment = segments[i++].Replace("~1", "/").Replace("~0", "~");

            switch (segment)
            {
                case "$defs":
                {
                    var key = segments[i++].Replace("~1", "/").Replace("~0", "~");
                    var defs =
                        current.GetKeyword<DefsKeyword>()?.Definitions
                        ?? throw new InvalidOperationException($"Schema has no $defs (reference: '{reference}')");
                    if (!defs.TryGetValue(key, out var def))
                        throw new InvalidOperationException($"$defs has no key '{key}' (reference: '{reference}')");
                    current = def;
                    break;
                }
                case "properties":
                {
                    var key = segments[i++].Replace("~1", "/").Replace("~0", "~");
                    var props =
                        current.GetKeyword<PropertiesKeyword>()?.Properties
                        ?? throw new InvalidOperationException($"Schema has no properties (reference: '{reference}')");
                    if (!props.TryGetValue(key, out var prop))
                        throw new InvalidOperationException(
                            $"properties has no key '{key}' (reference: '{reference}')"
                        );
                    current = prop;
                    break;
                }
                case "allOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<AllOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no allOf (reference: '{reference}')");
                    current = schemas[idx];
                    break;
                }
                case "oneOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<OneOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no oneOf (reference: '{reference}')");
                    current = schemas[idx];
                    break;
                }
                case "anyOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<AnyOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no anyOf (reference: '{reference}')");
                    current = schemas[idx];
                    break;
                }
                case "items":
                {
                    current =
                        current.GetKeyword<ItemsKeyword>()?.SingleSchema
                        ?? throw new InvalidOperationException($"Schema has no items (reference: '{reference}')");
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Cannot resolve JSON reference segment '{segment}' in '{reference}'"
                    );
            }
        }

        return current;
    }
}
