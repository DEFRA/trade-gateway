#r "nuget: Microsoft.CodeAnalysis.CSharp, 5.3.0"
#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (Args.Count < 2)
{
    Console.Error.WriteLine("Usage: FileSplitter.csx <input-file> <output-directory>");
    Environment.Exit(1);
}

var inputFile = Args[0];
var outputDir = Args[1];

if (!File.Exists(inputFile))
{
    Console.Error.WriteLine($"Input file not found: {inputFile}");
    Environment.Exit(1);
}

var sourceText = await File.ReadAllTextAsync(inputFile);
var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync();

var nsDecl = root.Members.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
if (nsDecl == null)
{
    Console.Error.WriteLine("No namespace declaration found in input file.");
    Environment.Exit(1);
}

var namespaceName = nsDecl.Name.ToString();
var fileHeader = nsDecl.GetLeadingTrivia().ToFullString();

var typeCount = 0;
var seen = new HashSet<string>();

foreach (var member in nsDecl.Members)
{
    string? typeName = member switch
    {
        BaseTypeDeclarationSyntax t => t.Identifier.ValueText,
        DelegateDeclarationSyntax d => d.Identifier.ValueText,
        _ => null,
    };

    if (typeName == null)
        continue;

    var fileName = typeName;
    if (!seen.Add(typeName))
    {
        var count = seen.Count(n => n == typeName || n.StartsWith(typeName + "_"));
        fileName = $"{typeName}_{count}";
    }

    var subdir = GetSubdirectory(typeName, member);
    var targetDir = Path.Combine(outputDir, subdir);
    Directory.CreateDirectory(targetDir);

    var cleanMember = member
        .WithLeadingTrivia(SyntaxFactory.LineFeed)
        .WithTrailingTrivia(SyntaxFactory.LineFeed);

    var newNs = SyntaxFactory
        .NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
        .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(cleanMember))
        .WithLeadingTrivia(SyntaxFactory.LineFeed)
        .WithTrailingTrivia(SyntaxFactory.LineFeed);

    var compilationUnit = SyntaxFactory
        .CompilationUnit()
        .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(newNs))
        .NormalizeWhitespace("    ", "\n", elasticTrivia: false);

    var content = fileHeader.TrimEnd() + "\n" + compilationUnit.ToFullString();
    var fullFileName = $"{fileName}.g.cs";
    var outputPath = Path.Combine(targetDir, fullFileName);
    await File.WriteAllTextAsync(outputPath, content);
    typeCount++;
    Console.WriteLine($"  {subdir}/{fullFileName}");
}

Console.WriteLine($"Split {typeCount} types into {outputDir}");

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static string GetSubdirectory(string typeName, MemberDeclarationSyntax member)
{
    // WCF service infrastructure — ordered most-specific first
    if (typeName.EndsWith("PortClient"))
        return "Clients";
    if (typeName.EndsWith("PortChannel"))
        return "Channels";
    if (typeName.EndsWith("Port"))
        return "Ports";

    // Fault types
    if (typeName.Contains("Exception"))
        return "Exceptions";

    // UN/CEFACT SPS domain model — any type whose name contains "SPS"
    //if (typeName.Contains("SPS"))         return "Sps";

    // XML Digital Signature types — detected via their W3C XML namespace
    if (GetXmlTypeNamespace(member) == "http://www.w3.org/2000/09/xmldsig#")
        return "Xmldsig";

    // SOAP message envelopes
    if (typeName.EndsWith("Request"))
        return "Messages";
    if (typeName.EndsWith("RequestType"))
        return "Messages";
    if (typeName.EndsWith("Response"))
        return "Messages";
    if (typeName.EndsWith("ResponseType"))
        return "Messages";

    return "Types";
}

// Reads the Namespace argument from the first XmlTypeAttribute or XmlRootAttribute on the member.
static string GetXmlTypeNamespace(MemberDeclarationSyntax member)
{
    foreach (var attrList in member.AttributeLists)
    foreach (var attr in attrList.Attributes)
    {
        var name = attr.Name.ToString();
        if (!name.Contains("XmlType") && !name.Contains("XmlRoot"))
            continue;

        var nsArg = attr.ArgumentList?.Arguments.FirstOrDefault(a =>
            a.NameEquals?.Name.Identifier.ValueText == "Namespace"
        );

        if (nsArg?.Expression is LiteralExpressionSyntax lit)
            return lit.Token.ValueText;
    }

    return string.Empty;
}
