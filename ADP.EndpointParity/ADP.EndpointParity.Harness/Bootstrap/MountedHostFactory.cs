using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
//
// This file holds NO group reference. The group-specific call -
// services.Add<Group>ApiServices<ParityDb>(mvcBuilder, configure) - is injected by the per-group
// test project as a delegate, which is what keeps the harness library group-free while still
// booting a group.
// ============================================================================================

/// <summary>
/// Boots a group that has no sample host, through its own public
/// <c>Add&lt;Group&gt;ApiServices&lt;TDbContext&gt;(mvcBuilder, configure)</c> entry point - the same
/// one a tenant uses. Not a mock, not a reimplementation.
///
/// <para>
/// <b>But it is one notch below a sample host, and here is the notch:</b> it does not reproduce
/// a consumer's middleware order, request localization, CORS, fallback routing, dashboard
/// hosting, or JSON options a real host might override. <b>A behaviour change hiding in host
/// wiring rather than in the module will not be caught.</b> For an upgrade whose risk is
/// concentrated in the mapper that is an acceptable trade; it would NOT be acceptable for an
/// upgrade touching serialization, routing or auth. Any step relying on this factory must say so
/// in its exit criteria rather than claiming full endpoint parity.
/// </para>
///
/// <para>
/// The wiring below is modelled on the repo's own minimal consumer,
/// <c>ADP.Darlastic.Sample.API/Program.cs</c>: AddControllers, AddShiftEntityWeb (hash id +
/// data assemblies), AddTypeAuth, then the module's own registration call.
/// </para>
/// </summary>
public static class MountedHostFactory
{
    /// <summary>
    /// Builds and starts the mounted host.
    /// </summary>
    /// <param name="databaseName">Disposable per-run database; created with EnsureCreated.</param>
    /// <param name="hashIdSalt">
    /// Pinned, not random. Rule 1 depends on it: same salt + same seeded long = same hash id, so
    /// seeded IDs compare literally across runs and a wrong ID is a diff rather than noise.
    /// </param>
    /// <param name="registerGroup">
    /// The group's own Add&lt;Group&gt;ApiServices call. Supplied by the per-group test project so
    /// this library needs no group reference.
    /// </param>
    /// <param name="registerActionTrees">The group's TypeAuth action trees.</param>
    public static async Task<WebApplication> StartAsync(
        string databaseName,
        string hashIdSalt,
        int hashIdMinLength,
        string issuer,
        Action<IServiceCollection, IMvcBuilder> registerGroup,
        Action<TypeAuthAspNetCoreOptions> registerActionTrees,
        string accessTreeJson,
        CancellationToken ct = default)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Environment.EnvironmentName = Environments.Development;

        // In-memory TestServer rather than Kestrel: the harness talks to this host through an
        // HttpClient with no socket, no port to allocate and no race between StartAsync and the
        // first request. It also matches how the sample-host groups are driven, so the two host
        // modes differ in what they WIRE, not in how they are observed.
        builder.WebHost.UseTestServer();

        var connection =
            "Server=localhost\\sqlexpress;Initial Catalog=" + databaseName +
            ";Persist Security Info=True;Integrated Security=SSPI;TrustServerCertificate=True;";

        builder.Services.AddDbContext<ParityDb>(o => o.UseSqlServer(connection));
        builder.Services.AddLocalization();
        builder.Services.AddHttpContextAccessor();

        // Required by ShiftEntity.Web internals - the Surveys sample registers it with the same
        // comment. Without it the inherited print-token route 500s with "No service for type
        // ShiftEntityPrintOptions has been registered", which is a MISSING-WIRING failure in the
        // mounted host rather than anything the upgrade could change.
        builder.Services.AddShiftEntityPrint(x =>
        {
            x.TokenExpirationInSeconds = 600;
            x.SASTokenKey = "parity-print-key";
        });

        var mvcBuilder = builder.Services.AddControllers();

        mvcBuilder.AddShiftEntityWeb(x =>
        {
            x.WrapValidationErrorResponseWithShiftEntityResponse(true);
            x.HashId.RegisterHashId(acceptUnencodedIds: true);

            // BOTH registrations, as every real host does. The identity hash service is a
            // SEPARATE registration with its own salt, and it is what decodes the principal's
            // CompanyId / CountryId / BranchId claims. Without it those claims do not resolve,
            // the framework's default data-level filters admit nothing, and every list for an
            // entity carrying a CompanyID column comes back {"Count":0,"Value":[]} with a 200 -
            // a baseline that looks healthy and proves nothing.
            x.HashId.RegisterIdentityHashId(acceptUnencodedIds: true);
        });

        // ShiftEntity's data-level access resolves ITypeAuthService, so this must be registered
        // even where the module's own action-tree enforcement is off.
        builder.Services.AddTypeAuth(registerActionTrees);

        // A mounted host has no identity server, but the module's controllers are [Authorize].
        // Without a scheme every request fails "No authenticationScheme was specified" - which is
        // how the first mounted capture came back: ten 5xx and no usable transcript. The handler
        // presents the same claim set a real token carries, INCLUDING the TypeAuth access tree,
        // so the FullAccess and Restricted passes differ here exactly as they do on a sample host.
        builder.Services.AddSingleton(new ParityPrincipalOptions { AccessTreeJson = accessTreeJson });
        builder.Services
            .AddAuthentication(ParityAuthenticationHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                       ParityAuthenticationHandler>(ParityAuthenticationHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();

        registerGroup(builder.Services, mvcBuilder);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // No MapFallbackToFile here - deliberately. A mounted host answers a missing route with a
        // real 404, unlike the two sample hosts. The global "no text/html body" assertion in
        // ParityRunner still applies to every group, because it is cheaper to keep one rule than
        // to reason per-group about which hosts carry the fallback hazard.

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ParityDb>();
            await db.Database.EnsureCreatedAsync(ct);
        }

        await app.StartAsync(ct);
        return app;
    }
}
