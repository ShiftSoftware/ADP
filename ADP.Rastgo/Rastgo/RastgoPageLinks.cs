namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Links shared by the Rastgo pages and their machine-readable authoring context.</summary>
public sealed record RastgoPageLinks(string Dashboard, string Trends, string Authoring)
{
    public static readonly RastgoPageLinks Default = new("dashboard", "trends", "docs");
    public static readonly RastgoPageLinks StaticFiles = new("dashboard.html", "trends.html", "authoring.html");
}
