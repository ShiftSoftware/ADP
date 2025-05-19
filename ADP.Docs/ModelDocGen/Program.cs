using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ShiftSoftware.ADP.Models;
using System.Text;


var baseDir = AppContext.BaseDirectory;
var modelFiles = Directory.GetFiles(Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "../../../../../ADP.Models"))), "*.cs", SearchOption.AllDirectories);

foreach (var file in modelFiles)
{
    var source = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(source);
    var root = tree.GetCompilationUnitRoot();

    var compilation = CSharpCompilation.Create("Temp")
        .AddSyntaxTrees(tree)
        .AddReferences(
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DocableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DocIgnoreAttribute).Assembly.Location)
        );

    var semanticModel = compilation.GetSemanticModel(tree);

    //var semanticModel = compilation.GetSemanticModel(tree);

    var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

    if (classDecl is null)
    {
        //Console.WriteLine("No class found.");
        continue;
    }

    var docableSymbol = semanticModel.GetDeclaredSymbol(classDecl);
    var docableAttr = docableSymbol?.GetAttributes().FirstOrDefault(a =>
        a.AttributeClass?.Name == "DocableAttribute" ||
        a.AttributeClass?.ToDisplayString() == "ShiftSoftware.ADP.Models.DocableAttribute");

    if (docableAttr == null) continue;

    var destinationPath = file.Contains("") ? file.Substring(file.IndexOf(@"\ADP.Models") + 12) : ""; // Handle Later

    var sb = new StringBuilder();
    sb.AppendLine();

    var classSummary = GetSyntaxNodeSummary(classDecl);
    if (!string.IsNullOrWhiteSpace(classSummary))
    {
        sb.AppendLine(EscapeMarkdown(classSummary));
        sb.AppendLine();
    }

    sb.AppendLine("| Property | Summary |");
    sb.AppendLine("|----------|---------|");

    foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
    {

        var symbol = semanticModel.GetDeclaredSymbol(prop);
        var hasDocIgnore = symbol?.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "DocIgnoreAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "ShiftSoftware.ADP.Models.DocIgnoreAttribute") == true;

        if (hasDocIgnore)
            continue;

        var name = prop.Identifier.Text;
        var type = prop.Type.ToString();
        var summary = EscapeMarkdown(GetSummary(prop));
        sb.AppendLine($"| {name} <div><strong>``{type}``</strong></div> | {summary} |");
    }

    destinationPath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../Docs/docs/generated/{destinationPath}"));

    //Replace file extension with .md
    destinationPath = Path.ChangeExtension(destinationPath, ".md");

    //Create the directory if it doesn't exist
    var directory = Path.GetDirectoryName(destinationPath);

    Directory.CreateDirectory(directory!);

    File.WriteAllText(destinationPath, sb.ToString());
}


string EscapeMarkdown(string input) => input.Replace("|", "\\|");

static string GetSummary(SyntaxNode node)
{
    foreach (var trivia in node.GetLeadingTrivia())
    {
        var structure = trivia.GetStructure();
        if (structure is DocumentationCommentTriviaSyntax docComment)
        {
            var summary = docComment.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            if (summary != null)
            {
                var sb = new StringBuilder();

                foreach (var c in summary.Content)
                {
                    if (c is XmlTextSyntax text)
                    {
                        // Combine all the tokens, trimming /// and newlines
                        var cleanText = string.Concat(text.TextTokens.Select(t => t.Text));
                        sb.Append(cleanText);
                    }
                    else if (c is XmlElementSyntax element && element.StartTag.Name.ToString() == "see")
                    {
                        var crefAttr = element.StartTag.Attributes
                            .OfType<XmlCrefAttributeSyntax>()
                            .FirstOrDefault();

                        var cref = crefAttr?.Cref.ToFullString().Trim('"') ?? "";
                        var linkText = string.Concat(element.Content.Select(e => e.ToFullString())).Trim();

                        if (!string.IsNullOrEmpty(cref))
                        {
                            sb.Append($"[{linkText}]({cref.ToString()}.html)");
                        }
                    }
                }

                return sb.ToString().Trim();
            }
        }
    }

    return "";
}

string? GetSyntaxNodeSummary(SyntaxNode node)
{
     foreach (var trivia in node.GetLeadingTrivia())
    {
        var structure = trivia.GetStructure();
        if (structure is DocumentationCommentTriviaSyntax docComment)
        {
            var summaryElement = docComment.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            if (summaryElement != null)
            {
                // Extract only the text content from the summary
                var text = string.Concat(summaryElement.Content
                    .OfType<XmlTextSyntax>()
                    .SelectMany(t => t.TextTokens)
                    .Select(t => t.Text));

                return text.Trim();
            }
        }
    }
    return null;
}