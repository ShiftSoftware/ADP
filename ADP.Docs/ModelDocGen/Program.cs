using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ShiftSoftware.ADP.Models.Part;
using System.Text;

var modelMapping = new Dictionary<string, string>
{
    { $"ADP.Models/Models/Part/{nameof(CatalogPartModel)}.cs", $"Docs/docs/generated/{nameof(CatalogPartModel)}.md" },
    { $"ADP.Models/Models/Part/{nameof(InvoicePartLineModel)}.cs", $"Docs/docs/generated/{nameof(InvoicePartLineModel)}.md" },
};

var baseDir = AppContext.BaseDirectory;

foreach (var model in modelMapping)
{
    var filePath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../../{model.Key}"));

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
    sb.AppendLine();
    sb.AppendLine("| Property | Summary |");
    sb.AppendLine("|----------|---------|");

    foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
    {
        var name = prop.Identifier.Text;
        var type = prop.Type.ToString();
        var summary = EscapeMarkdown(GetSummary(prop));
        sb.AppendLine($"| {name} <strong style='float: right;'>``{type}``</strong> | {summary} |");
    }

    File.WriteAllText(Path.GetFullPath(Path.Combine(baseDir, $"../../../../{model.Value}")), sb.ToString());
}

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