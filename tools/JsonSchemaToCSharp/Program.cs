using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Json.Schema;
using JsonSchemaToCSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;

var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../.."));

var named = ParseArgs(args);
var controlArg = named.GetValueOrDefault("--control-file");
if (string.IsNullOrWhiteSpace(controlArg))
{
    await Console.Error.WriteLineAsync("Missing required --control-file <path> argument");
    return 1;
}

var controlFile = Path.IsPathRooted(controlArg)
    ? Path.GetFullPath(controlArg)
    : Path.GetFullPath(Path.Combine(solutionRoot, controlArg));

if (!File.Exists(controlFile))
{
    await Console.Error.WriteLineAsync($"Control file not found: {controlFile}");
    return 1;
}

Console.WriteLine($"Using control file: {controlFile}");

IList<SchemaJob> jobs;
try
{
    var config = ParseConfiguration(controlFile);
    jobs = BuildJobs(config, solutionRoot);
}
catch (ValidationException vex)
{
    await Console.Error.WriteLineAsync($"Control file validation failed: {vex.Message}");
    return 1;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Failed to build jobs: {ex.Message}");
    return 1;
}

// Clear each distinct target output directory once to avoid stale files (do this before generation)
var outputDirectories = jobs.Select(j => j.OutputPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
foreach (var outputDirectory in outputDirectories)
{
    Directory.CreateDirectory(outputDirectory);
    var existingFiles = Directory.GetFiles(outputDirectory, "*.g.cs");
    foreach (var f in existingFiles)
        File.Delete(f);
    Console.WriteLine($"Cleaned {existingFiles.Length} existing generated files in {outputDirectory}");
}

var totalGenerated = 0;
var totalSkipped = 0;
var errors = new List<string>();
var generatedTypes = new HashSet<string>();

foreach (var job in jobs)
{
    var schemaFile = job.SchemaFile;
    var fileName = Path.GetFileName(schemaFile);
    Console.WriteLine($"Processing {fileName} -> {job.OutputPath} (ns: {job.Namespace})...");

    try
    {
        // Process the main schema and any referenced schema files (follow  file paths)
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toProcess = new Queue<string>();
        toProcess.Enqueue(schemaFile);

        while (toProcess.Count > 0)
        {
            var currentFile = toProcess.Dequeue();
            var currentFull = Path.GetFullPath(currentFile);
            if (!processedFiles.Add(currentFull))
                continue;

            var currentFileName = Path.GetFileName(currentFull);
            var currentDir = Path.GetDirectoryName(currentFull) ?? string.Empty;
            var currentRelative = Path.GetRelativePath(currentDir, currentFull);

            var loader = new SchemaLoader(currentDir);
            var generator = new CSharpGenerator(loader, job.Namespace);

            var schema = loader.LoadFile(currentRelative);

            // Generate types from  in this file
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

                    var syntax = generator.GenerateType(typeName, defSchema, currentRelative);
                    if (syntax is null)
                    {
                        totalSkipped++;
                        continue;
                    }

                    var outputFile = Path.Combine(job.OutputPath, $"{typeName}.g.cs");
                    await using var writer = new StreamWriter(outputFile, false);
                    // Roslyn's NormalizeWhitespace defaults to CRLF on every platform, so
                    // the newline has to be stated explicitly to match the repo's LF.
                    syntax
                        .NormalizeWhitespace(eol: "\n")
                        .WithTrailingTrivia(SyntaxFactory.ElasticLineFeed)
                        .WriteTo(writer);
                    totalGenerated++;
                }
            }

            // Generate root/document type for this file if applicable
            var rootTypeName = GetSchemaRootTypeName(currentFileName, schema);
            if (!generatedTypes.Contains(rootTypeName))
            {
                SyntaxNode? syntax = null;
                if (schema.GetKeyword<AllOfKeyword>() != null)
                    syntax = generator.GenerateDocumentType(rootTypeName, schema, currentRelative);
                if (syntax is null && schema.GetKeyword<TypeKeyword>()?.Type == SchemaValueType.Object)
                    syntax = generator.GenerateType(rootTypeName, schema, currentRelative);

                if (syntax is not null)
                {
                    var outputFile = Path.Combine(job.OutputPath, $"{rootTypeName}.g.cs");
                    await using var writer = new StreamWriter(outputFile, false);
                    // Roslyn's NormalizeWhitespace defaults to CRLF on every platform, so
                    // the newline has to be stated explicitly to match the repo's LF.
                    syntax
                        .NormalizeWhitespace(eol: "\n")
                        .WithTrailingTrivia(SyntaxFactory.ElasticLineFeed)
                        .WriteTo(writer);
                    totalGenerated++;
                    generatedTypes.Add(rootTypeName);
                }
                else if (defsKeyword == null)
                {
                    totalSkipped++;
                }
            }

            // Discover $ref file references and enqueue them if not already processed
            var text = await File.ReadAllTextAsync(currentFull);
            using var doc2 = JsonDocument.Parse(text);

            JsonParser.ProcessElement(doc2.RootElement, currentDir, processedFiles, toProcess);
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

static ControlConfig ParseConfiguration(string controlFile)
{
    var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddJsonFile(
        controlFile,
        optional: false,
        reloadOnChange: false
    );
    var configuration = builder.Build();
    var config = configuration.Get<ControlConfig>();
    if (config == null)
        throw new InvalidOperationException("Control file could not be bound to ControlConfig");

    var failures = new List<ValidationResult>();
    var ctx = new ValidationContext(config);
    Validator.TryValidateObject(config, ctx, failures, validateAllProperties: true);

    foreach (var group in config.Schemas)
    {
        var groupCtx = new ValidationContext(group);
        Validator.TryValidateObject(group, groupCtx, failures, validateAllProperties: true);
    }

    if (failures.Count > 0)
        throw new ValidationException(string.Join("; ", failures.Select(f => f.ErrorMessage)));

    return config;
}

static IList<SchemaJob> BuildJobs(ControlConfig config, string solutionRoot)
{
    var jobs = new List<SchemaJob>();
    var resolvedOutputRoot = Path.GetFullPath(Path.Combine(solutionRoot, config.OutputRoot));

    foreach (var group in config.Schemas)
    {
        var nsRoot = config.NamespaceRoot;
        var groupNs = string.IsNullOrWhiteSpace(nsRoot) ? group.Namespace : $"{nsRoot}.{group.Namespace}";
        var outputPath = Path.Combine(resolvedOutputRoot, group.Namespace);

        foreach (var schemaRel in group.SchemaItems)
        {
            var resolvedSchemaFile = Path.GetFullPath(Path.Combine(solutionRoot, schemaRel));
            if (!File.Exists(resolvedSchemaFile))
                throw new FileNotFoundException($"Schema file not found: {resolvedSchemaFile}", resolvedSchemaFile);

            jobs.Add(new SchemaJob(resolvedSchemaFile, outputPath, groupNs));
        }
    }

    return jobs;
}
