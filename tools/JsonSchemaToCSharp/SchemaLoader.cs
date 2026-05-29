using Json.Schema;

namespace JsonSchemaToCSharp;

public record ResolvedRef(JsonSchema Schema, string SourceFile);

public class SchemaLoader
{
    private readonly string _basePath;
    private readonly Dictionary<string, JsonSchema> _cache = new();

    public SchemaLoader(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
    }

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

    public ResolvedRef Resolve(string refPath, string currentFile)
    {
        string filePart;
        string pointer;

        if (refPath.Contains('#'))
        {
            var parts = refPath.Split('#', 2);
            filePart = parts[0];
            pointer = parts[1];
        }
        else
        {
            filePart = refPath;
            pointer = "";
        }

        string resolvedPath;
        JsonSchema root;
        if (string.IsNullOrEmpty(filePart))
        {
            resolvedPath = NormalizePath(currentFile);
            root = LoadFile(resolvedPath);
        }
        else
        {
            var currentDir = Path.GetDirectoryName(currentFile) ?? string.Empty;
            resolvedPath = NormalizePath(Path.Combine(currentDir, filePart));
            root = LoadFile(resolvedPath);
        }

        if (string.IsNullOrEmpty(pointer) || pointer == "/")
            return new ResolvedRef(root, resolvedPath);

        return new ResolvedRef(ResolvePointer(root, pointer), resolvedPath);
    }

    private string NormalizePath(string path)
    {
        var candidate = path.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(_basePath, candidate));
    }

    private static JsonSchema ResolvePointer(JsonSchema schema, string pointer)
    {
        var segments = pointer.TrimStart('/').Split('/');
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
                        ?? throw new InvalidOperationException($"Schema has no $defs (pointer: '{pointer}')");
                    if (!defs.TryGetValue(key, out var def))
                        throw new InvalidOperationException($"$defs has no key '{key}' (pointer: '{pointer}')");
                    current = def;
                    break;
                }
                case "properties":
                {
                    var key = segments[i++].Replace("~1", "/").Replace("~0", "~");
                    var props =
                        current.GetKeyword<PropertiesKeyword>()?.Properties
                        ?? throw new InvalidOperationException($"Schema has no properties (pointer: '{pointer}')");
                    if (!props.TryGetValue(key, out var prop))
                        throw new InvalidOperationException($"properties has no key '{key}' (pointer: '{pointer}')");
                    current = prop;
                    break;
                }
                case "allOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<AllOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no allOf (pointer: '{pointer}')");
                    current = schemas[idx];
                    break;
                }
                case "oneOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<OneOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no oneOf (pointer: '{pointer}')");
                    current = schemas[idx];
                    break;
                }
                case "anyOf":
                {
                    var idx = int.Parse(segments[i++]);
                    var schemas =
                        current.GetKeyword<AnyOfKeyword>()?.Schemas
                        ?? throw new InvalidOperationException($"Schema has no anyOf (pointer: '{pointer}')");
                    current = schemas[idx];
                    break;
                }
                case "items":
                {
                    current =
                        current.GetKeyword<ItemsKeyword>()?.SingleSchema
                        ?? throw new InvalidOperationException($"Schema has no items (pointer: '{pointer}')");
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Cannot resolve JSON pointer segment '{segment}' in '{pointer}'"
                    );
            }
        }

        return current;
    }
}
