using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ShiftSoftware.ADP.Models.Invoice;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text;

var modelMapping = new Dictionary<string, string>
{
    { $"ADP.Models/Models/Part/{nameof(CatalogPartModel)}.cs", $"Docs/docs/generated/part/{nameof(CatalogPartModel)}.md" },
    { $"ADP.Models/Models/Part/{nameof(CountryDataModel)}.cs", $"Docs/docs/generated/part/{nameof(CountryDataModel)}.md" },
    { $"ADP.Models/Models/Part/{nameof(InvoicePartLineModel)}.cs", $"Docs/docs/generated/part/{nameof(InvoicePartLineModel)}.md" },
    { $"ADP.Models/Models/Part/{nameof(RegionPriceModel)}.cs", $"Docs/docs/generated/part/{nameof(RegionPriceModel)}.md" },
    { $"ADP.Models/Models/Part/{nameof(StockPartModel)}.cs", $"Docs/docs/generated/part/{nameof(StockPartModel)}.md" },

    { $"ADP.Models/Models/Service/{nameof(FlatRateModel)}.cs", $"Docs/docs/generated/service/{nameof(FlatRateModel)}.md" },
    { $"ADP.Models/Models/Service/{nameof(InvoiceLaborLineModel)}.cs", $"Docs/docs/generated/service/{nameof(InvoiceLaborLineModel)}.md" },

    { $"ADP.Models/Models/Invoice/{nameof(InvoiceModel)}.cs", $"Docs/docs/generated/invoice/{nameof(InvoiceModel)}.md" },

    { $"ADP.Models/Models/Vehicle/{nameof(ColorModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(ColorModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(FreeServiceItemDateShiftModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(FreeServiceItemDateShiftModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(FreeServiceItemExcludedVINModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(FreeServiceItemExcludedVINModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(InitialOfficialVINModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(InitialOfficialVINModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(ItemClaimModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(ItemClaimModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(PaidServiceInvoiceLineModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(PaidServiceInvoiceLineModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(PaintThicknessInspectionModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(PaintThicknessInspectionModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(PaintThicknessPartModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(PaintThicknessPartModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(ServiceCampaignModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(ServiceCampaignModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(ServiceItemCostModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(ServiceItemCostModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(ServiceItemModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(ServiceItemModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(SSCAffectedVINModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(SSCAffectedVINModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(VehicleAccessoryModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(VehicleAccessoryModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(VehicleEntryModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(VehicleEntryModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(VehicleInspectionModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(VehicleInspectionModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(VehicleModelModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(VehicleModelModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(VehicleServiceActivation)}.cs", $"Docs/docs/generated/vehicle/{nameof(VehicleServiceActivation)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(WarrantyClaimLaborLineModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(WarrantyClaimLaborLineModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(WarrantyClaimModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(WarrantyClaimModel)}.md" },
    { $"ADP.Models/Models/Vehicle/{nameof(WarrantyDateShiftModel)}.cs", $"Docs/docs/generated/vehicle/{nameof(WarrantyDateShiftModel)}.md" },
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