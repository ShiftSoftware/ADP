using System.Text;

var baseDir = AppContext.BaseDirectory;
var featuresDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../../ADP.LookupServices.BDD/Features"));
var outputDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../Docs/docs/generated/Features"));

if (!Directory.Exists(featuresDir))
{
    Console.WriteLine($"Features directory not found: {featuresDir}");
    return;
}

Directory.CreateDirectory(outputDir);

var featureFiles = Directory.GetFiles(featuresDir, "*.feature");

foreach (var featureFile in featureFiles)
{
    var fileName = Path.GetFileNameWithoutExtension(featureFile);
    var content = File.ReadAllText(featureFile);

    var sb = new StringBuilder();

    sb.AppendLine("---");
    sb.AppendLine("hide:");
    sb.AppendLine("    - toc");
    sb.AppendLine("---");
    sb.AppendLine();
    sb.AppendLine("```gherkin");
    sb.Append(content);

    if (!content.EndsWith("\n"))
        sb.AppendLine();

    sb.AppendLine("```");

    var outputPath = Path.Combine(outputDir, $"{fileName}.md");
    File.WriteAllText(outputPath, sb.ToString());

    Console.WriteLine($"Generated: {outputPath}");
}
