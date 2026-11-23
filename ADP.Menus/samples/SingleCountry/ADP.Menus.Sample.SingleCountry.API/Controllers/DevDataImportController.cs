using Microsoft.AspNetCore.Mvc;

using ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.DevTools;
using ShiftSoftware.ADP.Menus.Sample.SingleCountry.Web.DTOs.DevTools;

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Controllers;

/// <summary>
/// DEVELOPMENT-ONLY endpoints that repopulate the sample database from another local database.
///
/// Sample-project scaffolding for manual testing — deliberately NOT part of any shipped ADP.Menus
/// package. Every action returns 404 outside the Development environment, so it cannot be reached in
/// a deployed sample.
///
/// The source database comes from the usual ConnectionStrings section
/// (<c>MenuImportSource</c>); unset, it disables the button.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class DevDataImportController : ControllerBase
{
    private const string MenuSchema = "Menu";

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
            MenuConfigured = !string.IsNullOrWhiteSpace(Source(MenuSourceName)),
            ConfigurationHint = $"Set ConnectionStrings:{MenuSourceName} in appsettings.Development.json.",
        });
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
