using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

var baseDir = AppContext.BaseDirectory;

var filePath = Path.GetFullPath(Path.Combine(baseDir, "../../../../../ADP.Models/Models/Part/CatalogPartModel.cs"));

var source = File.ReadAllText(filePath);
var tree = CSharpSyntaxTree.ParseText(source);
var root = tree.GetCompilationUnitRoot();

var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
if (classDecl is null)
{
    Console.WriteLine("No class found.");
    return;
}

var sb = new StringBuilder();
sb.AppendLine($"# {classDecl.Identifier.Text}");
sb.AppendLine();
sb.AppendLine("| Property | Type | Summary |");
sb.AppendLine("|----------|------|---------|");

foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
{
    var name = prop.Identifier.Text;
    var type = prop.Type.ToString();
    var summary = EscapeMarkdown(GetSummary(prop));
    sb.AppendLine($"| {name} | {type} | {summary} |");
}

File.WriteAllText(Path.GetFullPath(Path.Combine(baseDir, "../../../../Docs/docs/generated/PartCatalog.md")), sb.ToString());


string EscapeMarkdown(string input) => input.Replace("|", "\\|");

string GetSummary(PropertyDeclarationSyntax prop)
{
    foreach (var trivia in prop.GetLeadingTrivia())
    {
        var structure = trivia.GetStructure();
        if (structure is DocumentationCommentTriviaSyntax docComment)
        {
            var summaryElement = docComment.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            if (summaryElement != null)
            {
                var text = summaryElement.Content
                    .OfType<XmlTextSyntax>()
                    .SelectMany(t => t.TextTokens)
                    .Where(token => token.IsKind(SyntaxKind.XmlTextLiteralToken))
                    .Select(token => token.Text.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t));

                return string.Join(" ", text);
            }
        }
    }

    return "";
}