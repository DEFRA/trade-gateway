using Json.Schema;
using JsonSchemaToCSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));

var named = ParseArgs(args);

var schemaBasePath = named.GetValueOrDefault("--schema") ?? Path.Combine(solutionRoot, "schema");
var outputPath = named.GetValueOrDefault("--output") ?? Path.Combine(solutionRoot, "schema-output");
var outputNamespace = named.GetValueOrDefault("--namespace") ?? "Api.Models.Unece";

var resolvedSchemaPath = Path.GetFullPath(schemaBasePath);
var resolvedOutputPath = Path.GetFullPath(outputPath);

Console.WriteLine($"Schema path: {resolvedSchemaPath}");
Console.WriteLine($"Output path: {resolvedOutputPath}");

if (!Directory.Exists(resolvedSchemaPath))
{
    await Console.Error.WriteLineAsync($"Schema directory not found: {resolvedSchemaPath}");
    return 1;
}

Directory.CreateDirectory(resolvedOutputPath);

var existingFiles = Directory.GetFiles(resolvedOutputPath, "*.g.cs");
foreach (var file in existingFiles)
    File.Delete(file);

Console.WriteLine($"Cleaned {existingFiles.Length} existing generated files");

var loader = new SchemaLoader(resolvedSchemaPath);
var generator = new CSharpGenerator(loader, outputNamespace);

var schemaFiles = Directory.GetFiles(resolvedSchemaPath, "*.schema.json");
if (schemaFiles.Length == 0)
    schemaFiles = Directory.GetFiles(resolvedSchemaPath, "*.json");

var totalGenerated = 0;
var totalSkipped = 0;
var errors = new List<string>();
var generatedTypes = new HashSet<string>();

foreach (var schemaFile in schemaFiles.OrderBy(f => f))
{
    var fileName = Path.GetFileName(schemaFile);
    Console.WriteLine($"Processing {fileName}...");

    try
    {
        var relativeFile = Path.GetRelativePath(resolvedSchemaPath, schemaFile);
        var schema = loader.LoadFile(relativeFile);

        var defsKeyword = schema.GetKeyword<DefsKeyword>();
        if (defsKeyword != null)
        {
            foreach (var (defName, defSchema) in defsKeyword.Definitions)
            {
                if (defSchema.GetKeyword<TypeKeyword>()?.Type != SchemaValueType.Object)
                    continue;

                if (defSchema.GetKeyword<PropertiesKeyword>() == null)
                    continue;

                var typeName = TypeNameResolver.DefKeyToCSharpName(defName);
                if (!generatedTypes.Add(typeName))
                    continue;

                try
                {
                    var syntax = generator.GenerateType(typeName, defSchema, relativeFile);
                    if (syntax is null)
                    {
                        totalSkipped++;
                        continue;
                    }

                    var outputFile = Path.Combine(resolvedOutputPath, $"{typeName}.g.cs");
                    await using var writer = new StreamWriter(outputFile, false);
                    syntax
                        .NormalizeWhitespace()
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                        .WriteTo(writer);
                    totalGenerated++;
                }
                catch (Exception ex)
                {
                    errors.Add($"  Error generating {defName}: {ex.Message}");
                }
            }
        }

        var rootTypeName = GetSchemaRootTypeName(fileName, schema);
        if (!generatedTypes.Contains(rootTypeName))
        {
            Microsoft.CodeAnalysis.SyntaxNode? syntax = null;

            if (schema.GetKeyword<AllOfKeyword>() != null)
                syntax = generator.GenerateDocumentType(rootTypeName, schema, relativeFile);

            if (syntax is null && schema.GetKeyword<TypeKeyword>()?.Type == SchemaValueType.Object)
                syntax = generator.GenerateType(rootTypeName, schema, relativeFile);

            if (syntax is not null)
            {
                var outputFile = Path.Combine(resolvedOutputPath, $"{rootTypeName}.g.cs");
                await using var writer = new StreamWriter(outputFile, false);
                syntax
                    .NormalizeWhitespace()
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                    .WriteTo(writer);
                totalGenerated++;
                generatedTypes.Add(rootTypeName);
            }
            else if (defsKeyword == null)
            {
                totalSkipped++;
            }
        }
    }
    catch (Exception ex)
    {
        errors.Add($"Error processing {fileName}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Generated: {totalGenerated} files");
Console.WriteLine($"Skipped: {totalSkipped}");
Console.WriteLine($"Errors: {errors.Count}");

foreach (var error in errors.Take(20))
    Console.WriteLine(error);

if (errors.Count > 20)
    Console.WriteLine($"... and {errors.Count - 20} more errors");

return errors.Count > 0 ? 1 : 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>();
    var i = 0;
    while (i < args.Length - 1)
    {
        if (args[i].StartsWith("--"))
            result[args[i++]] = args[i++];
        else
            i++;
    }
    return result;
}

static string GetSchemaRootTypeName(string fileName, JsonSchema schema)
{
    var title = schema.GetKeyword<TitleKeyword>()?.Value;
    if (!string.IsNullOrWhiteSpace(title))
        return TypeNameResolver.ToCSharpTypeName(title);

    var name = Path.GetFileNameWithoutExtension(fileName);
    if (name.EndsWith(".schema", StringComparison.OrdinalIgnoreCase))
        name = name[..^".schema".Length];

    return TypeNameResolver.ToCSharpTypeName(name);
}
