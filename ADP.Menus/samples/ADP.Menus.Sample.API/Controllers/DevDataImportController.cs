using Microsoft.AspNetCore.Mvc;

using ShiftSoftware.ADP.Menus.Sample.API.DevTools;
using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.DevTools;

namespace ShiftSoftware.ADP.Menus.Sample.API.Controllers;

/// <summary>
/// DEVELOPMENT-ONLY endpoints that repopulate the sample database from another local database.
///
/// Sample-project scaffolding for manual testing — deliberately NOT part of any shipped ADP.Menus
/// package. Every action returns 404 outside the Development environment, so it cannot be reached in
/// a deployed sample.
///
/// Source databases come from the usual ConnectionStrings section
/// (<c>IdentityImportSource</c> / <c>MenuImportSource</c>); an unset one disables its button.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class DevDataImportController : ControllerBase
{
    private const string IdentitySchema = "ShiftIdentity";
    private const string MenuSchema = "Menu";

    private const string IdentitySourceName = "IdentityImportSource";
    private const string MenuSourceName = "MenuImportSource";

    private readonly SchemaCopyService schemaCopy;
    private readonly IWebHostEnvironment environment;
    private readonly IConfiguration configuration;

    public DevDataImportController(
        SchemaCopyService schemaCopy,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        this.schemaCopy = schemaCopy;
        this.environment = environment;
        this.configuration = configuration;
    }

    /// <summary>Which buttons the UI should enable — i.e. which sources are configured.</summary>
    [HttpGet("availability")]
    public ActionResult<DevDataImportAvailabilityDTO> GetAvailability()
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return Ok(new DevDataImportAvailabilityDTO
        {
            IdentityConfigured = !string.IsNullOrWhiteSpace(Source(IdentitySourceName)),
            MenuConfigured = !string.IsNullOrWhiteSpace(Source(MenuSourceName)),
            ConfigurationHint = $"Set ConnectionStrings:{IdentitySourceName} and ConnectionStrings:{MenuSourceName} in appsettings.Development.json.",
        });
    }

    /// <summary>
    /// Replaces every row in the [ShiftIdentity] schema with the configured source database's.
    ///
    /// The seeded superuser is preserved: rows with <c>IsProtected = 1</c> are neither deleted here nor
    /// copied from the source, so you can still sign in afterwards. Because the rest of identity is
    /// replaced, that surviving user's company/branch references may dangle until the API restarts and
    /// its startup seeder re-upserts them by name.
    /// </summary>
    [HttpPost("identity")]
    public async Task<ActionResult<DevDataImportResultDTO>> ImportIdentityAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var sourceConnectionString = Source(IdentitySourceName);
        if (string.IsNullOrWhiteSpace(sourceConnectionString))
            return BadRequest(Unconfigured(IdentitySourceName));

        var result = await schemaCopy.CopySchemaAsync(
            sourceConnectionString,
            TargetConnectionString,
            IdentitySchema,
            preserve: new Dictionary<string, SchemaCopyService.PreserveRule>(StringComparer.OrdinalIgnoreCase)
            {
                // The superuser is created by the startup seeder, not by this copy. BuiltIn is the
                // older name for the same flag, in case the source database predates the rename.
                ["Users"] = new SchemaCopyService.PreserveRule(
                    FlagColumn: "IsProtected",
                    KeyColumn: "ID",
                    LegacyFlagColumn: "BuiltIn"),
            },
            cancellationToken);

        if (result.Succeeded)
            result.Warnings.Add("Restart the API so its identity seeder re-links the preserved superuser to a company and branch.");

        return Ok(result);
    }

    /// <summary>Replaces every row in the [Menu] schema with the configured source database's.</summary>
    [HttpPost("menu")]
    public async Task<ActionResult<DevDataImportResultDTO>> ImportMenuAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var sourceConnectionString = Source(MenuSourceName);
        if (string.IsNullOrWhiteSpace(sourceConnectionString))
            return BadRequest(Unconfigured(MenuSourceName));

        var result = await schemaCopy.CopySchemaAsync(
            sourceConnectionString,
            TargetConnectionString,
            MenuSchema,
            preserve: null,
            cancellationToken);

        if (result.Succeeded)
            result.Warnings.Add("The sample's own menu seed data was replaced; it is re-seeded only into empty tables, so it will not come back on restart.");

        return Ok(result);
    }

    private string? Source(string name) => configuration.GetConnectionString(name);

    private string TargetConnectionString =>
        configuration.GetConnectionString("SQLServer")
        ?? throw new InvalidOperationException("ConnectionStrings:SQLServer is not configured.");

    private static DevDataImportResultDTO Unconfigured(string name) => new()
    {
        Succeeded = false,
        Message = $"ConnectionStrings:{name} is not set in appsettings.Development.json.",
    };
}
