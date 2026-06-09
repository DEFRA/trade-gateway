using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JsonSchemaToCSharp;

public class CSharpGenerator(SchemaLoader loader, string outputNamespace)
{
    public SyntaxNode? GenerateDocumentType(string typeName, JsonSchema schema, string sourceFile)
    {
        var allOfKeyword = schema.GetKeyword<AllOfKeyword>();
        if (allOfKeyword == null)
            return null;

        var mergedProperties = new Dictionary<string, (JsonSchema Value, string SourceFile)>();
        var required = new HashSet<string>();
        CollectAllOfProperties(allOfKeyword.Schemas, sourceFile, mergedProperties, required);

        if (mergedProperties.Count == 0)
            return null;

        var inlineTypeNames = new HashSet<string>(StringComparer.Ordinal);
        inlineTypeNames.Add(typeName);
        var additionalTypes = new List<MemberDeclarationSyntax>();

        var mainRecord = CreateRecordDeclarationFromProperties(
            typeName,
            mergedProperties,
            required,
            inlineTypeNames,
            additionalTypes
        );

        if (mainRecord is null)
            return null;

        return BuildCompilationUnit(mainRecord, additionalTypes);
    }

    private void CollectAllOfProperties(
        IReadOnlyList<JsonSchema> allOf,
        string sourceFile,
        Dictionary<string, (JsonSchema Value, string SourceFile)> properties,
        HashSet<string> required
    )
    {
        foreach (var item in allOf)
        {
            var refKeyword = item.GetKeyword<RefKeyword>();
            if (refKeyword != null)
            {
                var resolved = loader.Resolve(refKeyword.Reference.OriginalString, sourceFile);
                var target = resolved.Schema;
                var targetFile = resolved.SourceFile;

                // Follow root-level $ref chains (e.g. a schema file whose root is just
                // $ref: "#/$defs/SomeType" rather than defining properties directly)
                var rootRef = target.GetKeyword<RefKeyword>();
                while (
                    rootRef != null
                    && target.GetKeyword<PropertiesKeyword>() == null
                    && target.GetKeyword<AllOfKeyword>() == null
                )
                {
                    var chained = loader.Resolve(rootRef.Reference.OriginalString, targetFile);
                    target = chained.Schema;
                    targetFile = chained.SourceFile;
                    rootRef = target.GetKeyword<RefKeyword>();
                }

                var nestedAllOf = target.GetKeyword<AllOfKeyword>();
                if (nestedAllOf != null)
                    CollectAllOfProperties(nestedAllOf.Schemas, targetFile, properties, required);

                var requiredKeyword = target.GetKeyword<RequiredKeyword>();
                if (requiredKeyword != null)
                    foreach (var r in requiredKeyword.Properties)
                        required.Add(r);

                var refProps = target.GetKeyword<PropertiesKeyword>();
                if (refProps != null)
                {
                    foreach (var (propName, propSchema) in refProps.Properties)
                        properties[propName] = (propSchema, targetFile);
                }
            }

            var inlineProps = item.GetKeyword<PropertiesKeyword>();
            if (inlineProps != null)
            {
                foreach (var (propName, propSchema) in inlineProps.Properties)
                {
                    if (ReferenceEquals(propSchema, JsonSchema.False))
                        properties.Remove(propName);
                    else
                        properties[propName] = (propSchema, sourceFile);
                }
            }
        }
    }

    public SyntaxNode? GenerateType(string typeName, JsonSchema schema, string sourceFile)
    {
        if (schema.GetKeyword<TypeKeyword>()?.Type != SchemaValueType.Object)
            return null;

        if (schema.GetKeyword<PropertiesKeyword>() == null)
            return null;

        var inlineTypeNames = new HashSet<string>(StringComparer.Ordinal);
        inlineTypeNames.Add(typeName);
        var additionalTypes = new List<MemberDeclarationSyntax>();

        var mainRecord = CreateRecordDeclaration(typeName, schema, sourceFile, inlineTypeNames, additionalTypes);
        if (mainRecord is null)
            return null;

        return BuildCompilationUnit(mainRecord, additionalTypes);
    }

    private SyntaxNode BuildCompilationUnit(
        RecordDeclarationSyntax mainRecord,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(outputNamespace));
        var compilation = CompilationUnit()
            .AddUsings(
                CreateUsing("System.Text.Json"),
                CreateUsing("System.Text.Json.Serialization"),
                CreateUsing("System.ComponentModel"),
                CreateUsing("System.Collections.Generic")
            )
            .WithLeadingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)))
            .AddMembers(namespaceDeclaration)
            .AddMembers(mainRecord);

        if (additionalTypes.Count > 0)
            compilation = compilation.AddMembers(additionalTypes.ToArray());

        return compilation;
    }

    private RecordDeclarationSyntax? CreateRecordDeclarationFromProperties(
        string typeName,
        Dictionary<string, (JsonSchema Value, string SourceFile)> properties,
        HashSet<string> required,
        HashSet<string> inlineTypeNames,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        var memberList = new List<MemberDeclarationSyntax>();

        foreach (var (propName, (propSchema, propSourceFile)) in properties)
        {
            if (ReferenceEquals(propSchema, JsonSchema.False))
                continue;

            var csharpType = ResolvePropertyTypeName(
                propSchema,
                propSourceFile,
                typeName,
                propName,
                inlineTypeNames,
                additionalTypes
            );
            if (string.IsNullOrEmpty(csharpType))
                continue;

            var csharpPropName = TypeNameResolver.ToCSharpPropertyName(propName);
            if (csharpPropName == typeName)
                csharpPropName += "Value";

            var propertyDecl = CreateProperty(
                propName,
                csharpPropName,
                csharpType,
                required.Contains(propName),
                propSchema
            );
            memberList.Add(propertyDecl);
        }

        if (memberList.Count == 0)
            return null;

        return CreateRecord(typeName)
            .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
            .AddMembers(memberList.ToArray())
            .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken));
    }

    private RecordDeclarationSyntax? CreateRecordDeclaration(
        string typeName,
        JsonSchema schema,
        string sourceFile,
        HashSet<string> inlineTypeNames,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        var required = schema.GetKeyword<RequiredKeyword>()?.Properties?.ToHashSet() ?? [];

        var propertiesKeyword = schema.GetKeyword<PropertiesKeyword>();
        if (propertiesKeyword == null)
            return null;

        var properties = propertiesKeyword.Properties.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value, SourceFile: sourceFile)
        );

        return CreateRecordDeclarationFromProperties(typeName, properties, required, inlineTypeNames, additionalTypes);
    }

    private string ResolvePropertyTypeName(
        JsonSchema schema,
        string sourceFile,
        string parentTypeName,
        string propertyName,
        HashSet<string> inlineTypeNames,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        if (ReferenceEquals(schema, JsonSchema.False))
            return string.Empty;

        if (IsInlineObjectSchema(schema))
            return GenerateInlineObjectType(
                parentTypeName,
                propertyName,
                schema,
                sourceFile,
                inlineTypeNames,
                additionalTypes
            );

        var typeKeyword = schema.GetKeyword<TypeKeyword>();
        if (typeKeyword?.Type == SchemaValueType.Array)
            return ResolveArrayTypeName(
                schema,
                sourceFile,
                parentTypeName,
                propertyName,
                inlineTypeNames,
                additionalTypes
            );

        var resolver = new TypeNameResolver(loader, sourceFile);
        var resolved = resolver.ResolvePropertyTypeName(schema);

        if (resolved == DotNetTypes.Object && IsInlineObjectSchema(schema, allowAllOf: true))
            return GenerateInlineObjectType(
                parentTypeName,
                propertyName,
                schema,
                sourceFile,
                inlineTypeNames,
                additionalTypes
            );

        return resolved;
    }

    private string ResolveArrayTypeName(
        JsonSchema schema,
        string sourceFile,
        string parentTypeName,
        string propertyName,
        HashSet<string> inlineTypeNames,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        var items = schema.GetKeyword<ItemsKeyword>()?.SingleSchema;
        if (items == null)
            return $"List<{DotNetTypes.Object}>";

        var itemType = ResolvePropertyTypeName(
            items,
            sourceFile,
            parentTypeName,
            propertyName + "Item",
            inlineTypeNames,
            additionalTypes
        );
        return $"List<{itemType}>";
    }

    private static bool IsInlineObjectSchema(JsonSchema schema, bool allowAllOf = false)
    {
        var typeKeyword = schema.GetKeyword<TypeKeyword>();
        if (typeKeyword == null)
            return false;

        var isObject = typeKeyword.Type == SchemaValueType.Object || typeKeyword.Type.HasFlag(SchemaValueType.Object);

        if (!isObject)
            return false;

        if (schema.GetKeyword<PropertiesKeyword>() != null)
            return true;

        if (allowAllOf && schema.GetKeyword<AllOfKeyword>() != null)
            return true;

        return false;
    }

    private string GenerateInlineObjectType(
        string parentTypeName,
        string propertyName,
        JsonSchema schema,
        string sourceFile,
        HashSet<string> inlineTypeNames,
        List<MemberDeclarationSyntax> additionalTypes
    )
    {
        var inlineName = GetInlineTypeName(parentTypeName, propertyName, inlineTypeNames);
        var inlineRecord = CreateRecordDeclaration(inlineName, schema, sourceFile, inlineTypeNames, additionalTypes);
        if (inlineRecord is null)
            return DotNetTypes.Object;

        additionalTypes.Add(inlineRecord);
        return inlineName;
    }

    private static string GetInlineTypeName(string parentTypeName, string propertyName, HashSet<string> inlineTypeNames)
    {
        var baseName = parentTypeName + TypeNameResolver.ToCSharpPropertyName(propertyName);
        var candidate = baseName;
        var suffix = 1;

        while (!inlineTypeNames.Add(candidate))
        {
            candidate = baseName + suffix;
            suffix++;
        }

        return candidate;
    }

    private static PropertyDeclarationSyntax CreateProperty(
        string jsonName,
        string csharpPropertyName,
        string csharpType,
        bool isRequired,
        JsonSchema schema
    )
    {
        var constKeyword = schema.GetKeyword<ConstKeyword>();
        var constStringValue =
            constKeyword?.Value is System.Text.Json.Nodes.JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

        var typeSyntax = ParseTypeName(csharpType);
        var modifiers = new List<SyntaxToken> { Token(SyntaxKind.PublicKeyword) };

        if (constStringValue != null)
        {
            // const properties: non-nullable, no required modifier — default value handles initialisation
        }
        else if (isRequired)
        {
            modifiers.Add(Token(SyntaxKind.RequiredKeyword));
        }
        else
        {
            typeSyntax = NullableType(typeSyntax);
        }

        var attributes = new List<AttributeListSyntax> { CreateSimpleAttributeList("JsonPropertyName", jsonName) };

        var description = schema.GetKeyword<DescriptionKeyword>()?.Value;
        if (!string.IsNullOrEmpty(description))
            attributes.Add(CreateSimpleAttributeList("Description", description));

        if (constStringValue != null)
            attributes.Add(CreateSimpleAttributeList("ConstValue", constStringValue));

        var property = PropertyDeclaration(typeSyntax, csharpPropertyName)
            .AddModifiers(modifiers.ToArray())
            .AddAttributeLists(attributes.ToArray())
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );

        if (constStringValue != null)
        {
            property = property
                .WithInitializer(
                    EqualsValueClause(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(constStringValue)))
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        return property;
    }

    private static RecordDeclarationSyntax CreateRecord(string name) =>
        RecordDeclaration(Token(SyntaxKind.RecordKeyword), Identifier(name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword));

    private static UsingDirectiveSyntax CreateUsing(string fqn) => UsingDirective(ParseName(fqn));

    private static AttributeListSyntax CreateSimpleAttributeList(string type, string arg1) =>
        AttributeList(
            SingletonSeparatedList(
                Attribute(ParseName(type)).WithArgumentList(ParseAttributeArgumentList($"(\"{EscapeString(arg1)}\")"))
            )
        );

    private static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
