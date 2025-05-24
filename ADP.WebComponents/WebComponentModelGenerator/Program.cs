using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ShiftSoftware.ADP.Models;
using System.Text;

var baseDir = AppContext.BaseDirectory;
var modelFiles = Directory.GetFiles(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "../../../../../ADP.LookupServices/Lookup.Services"))), "*.cs", SearchOption.AllDirectories);

// Parse ALL files first
var syntaxTreeWithPaths = modelFiles
    .Select(file => (filePath: file, tree: CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)))
    .ToList();

var compilation = CSharpCompilation.Create("Temp")
    .AddSyntaxTrees(syntaxTreeWithPaths.Select(x => x.tree))
    .AddReferences(
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(TypeScriptModelAttribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location)
    );

foreach (var (file, tree) in syntaxTreeWithPaths)
{
    var semanticModel = compilation.GetSemanticModel(tree);
    
    var root = tree.GetCompilationUnitRoot();

    var destinationPath = file.Contains("Lookup.Services\\DTOsAndModel") ? file.Substring(file.IndexOf(@"\Lookup.Services\DTOsAndModel") + @"\Lookup.Services\DTOsAndModel".Length + 1) : ""; // Handle Later

    var sb = new StringBuilder();

    var typeDecl = root.DescendantNodes().FirstOrDefault(n => n is ClassDeclarationSyntax || n is EnumDeclarationSyntax);

    if (typeDecl == null) continue;

    var typeScriptModelSymbol = semanticModel.GetDeclaredSymbol(typeDecl);

    var typeScriptModel = typeScriptModelSymbol?.GetAttributes().FirstOrDefault(a =>
        a.AttributeClass?.Name == "TypeScriptModelAttribute" ||
        a.AttributeClass?.ToDisplayString() == "ShiftSoftware.ADP.Models.TypeScriptModelAttribute");

    if (typeScriptModel == null) continue;

    if (typeDecl is ClassDeclarationSyntax classDecl)
    {
        var referencedTypes = new HashSet<string>();
        var typeLines = new StringBuilder();

        foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(prop);

            var hasTypeScriptIgnore = symbol?.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == "TypeScriptIgnoreAttribute" ||
                attr.AttributeClass?.ToDisplayString() == "ShiftSoftware.ADP.Models.TypeScriptIgnoreAttribute") == true
                ;

            if (hasTypeScriptIgnore)
                continue;

            var tsType = GetTypescriptType(semanticModel, prop.Type);
            var name = prop.Identifier.Text;

            // Check if the type is a custom type (not primitive)
            if (!IsPrimitiveType(tsType) && !tsType.EndsWith("[]") && !IsInlineEnumType(tsType))
            {
                referencedTypes.Add(tsType.Replace("[]", ""));
            }
            else if (tsType.EndsWith("[]"))
            {
                var elementType = tsType.Replace("[]", "");
                if (!IsPrimitiveType(elementType) && !IsInlineEnumType(elementType))
                    referencedTypes.Add(elementType);
            }

            typeLines.AppendLine($"    {ToCamelCase(name)}{(prop.Type is NullableTypeSyntax ? "?" : "")}: {tsType};");
        }

        var tsTypeName = ToCamelCase(classDecl.Identifier.Text);

        // Generate imports only once, before the type definition
        var importSb = new StringBuilder();
        foreach (var refType in referencedTypes)
        {
            if (refType != classDecl.Identifier.Text && !IsInlineEnumType(refType))
                importSb.AppendLine($"import type {{ {ToCamelCase(refType)} }} from './{ToCamelCase(refType)}';");
        }

        sb.Append(importSb.ToString());
        sb.AppendLine($"export type {tsTypeName} = " + "{");
        sb.Append(typeLines.ToString());
        sb.Append("};");

        destinationPath = string.Join('\\',
            destinationPath
            .Split('\\')
            .Select(x => ToCamelCase(x))
        );

        destinationPath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../adp-web-components/src/global/types/{destinationPath}"));

        destinationPath = Path.ChangeExtension(destinationPath, ".ts");

        var directory = Path.GetDirectoryName(destinationPath);

        Directory.CreateDirectory(directory!);

        File.WriteAllText(destinationPath, sb.ToString());
    }
}

bool IsPrimitiveType(string tsType)
{
    return tsType is "number" or "string" or "boolean" or "any";
}

string GetTypescriptType(SemanticModel semanticModel, TypeSyntax type)
{
    // Normalize nullable types
    var nonNullableType = type is NullableTypeSyntax nullableType ? nullableType.ElementType : type;

    var typeInfo = semanticModel.GetTypeInfo(nonNullableType);
    var symbol = typeInfo.Type;

    return GetTypescriptTypeFromSymbol(symbol);
}

string GetTypescriptTypeFromSymbol(ITypeSymbol symbol)
{
    if (symbol == null) return "any";

    if (symbol is IArrayTypeSymbol arrayType)
    {
        return $"{GetTypescriptTypeFromSymbol(arrayType.ElementType)}[]";
    }

    switch (symbol.SpecialType)
    {
        case SpecialType.System_Int16:
        case SpecialType.System_Int32:
        case SpecialType.System_Int64:
        case SpecialType.System_Single:
        case SpecialType.System_Double:
        case SpecialType.System_Decimal:
            return "number";
        case SpecialType.System_String:
            return "string";
        case SpecialType.System_Boolean:
            return "boolean";
        case SpecialType.System_DateTime:
        //case SpecialType.System_Guid:
        //    return "string";
        case SpecialType.System_Object:
            return "any";
    }

    if (symbol.ToDisplayString() == "System.Guid")
        return "string";

    // byte[]
    if (symbol.ToDisplayString() == "byte[]")
        return "string"; // base64 encoded

    // Nullable<T>
    if (symbol.OriginalDefinition.ToDisplayString() == "System.Nullable<T>" &&
        symbol is INamedTypeSymbol nullableSymbol &&
        nullableSymbol.TypeArguments.Length == 1)
    {
        var inner = GetTypescriptTypeFromSymbol(nullableSymbol.TypeArguments[0]);
        return $"{inner} | null";
    }

    // Enum inline string union
    if (symbol is INamedTypeSymbol enumSymbol && enumSymbol.TypeKind == TypeKind.Enum)
    {
        var enumMembers = enumSymbol
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToArray();

        if (enumMembers.Length > 0)
        {
            return string.Join(" | ", enumMembers.Select(m => $"'{ToCamelCase(m.Name)}'"));
        }
    }

    // Collection types (IEnumerable<T>, List<T>, etc.)
    if (symbol is INamedTypeSymbol namedType &&
        namedType.IsGenericType &&
        namedType.AllInterfaces.Any(i => i.Name == "IEnumerable"))
    {
        var itemType = namedType.TypeArguments[0];
        return $"{GetTypescriptTypeFromSymbol(itemType)}[]";
    }

    // Otherwise — custom types: assume will be generated elsewhere
    return ToCamelCase(symbol.Name);
}

string ToCamelCase(string name)
{
    if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        return name;

    if (name.Length == 1)
        return name.ToLowerInvariant();

    // If the first two characters are uppercase
    if (char.IsUpper(name[0]) && char.IsUpper(name[1]))
    {
        // If the entire string is uppercase (e.g., "PNC")
        if (name.All(char.IsUpper))
        {
            return name.ToLowerInvariant();
        }

        // If it's a mixed acronym+word (e.g., "HSCode")
        int i = 2;
        while (i < name.Length && char.IsUpper(name[i]))
            i++;

        return name.Substring(0, i - 1).ToLowerInvariant() + name.Substring(i - 1);
    }

    // Standard PascalCase → camelCase
    return char.ToLowerInvariant(name[0]) + name.Substring(1);
}

bool IsInlineEnumType(string tsType)
{
    return tsType.Contains("'") && tsType.Contains("|");
}