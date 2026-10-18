using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Extensions;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// <c>GET GoldenCustomer</c> with an OData query the list cannot apply: a filter that compares a protected member
/// (a <c>PiiFieldDTO</c>: name, phone, e-mail, ID number) as if it were a string, or one that does not parse, is the
/// caller's mistake and answers 400 with a fixed message (never the query text back), not a 500.
/// </summary>
public class GoldenCustomerListFilterTests
{
    [Theory]
    [InlineData("FullName eq 'Synthname'")]
    [InlineData("Phone eq '0000 555 0101'")]
    [InlineData("contains(Email, 'example')")]
    [InlineData("IDNumber ne 'SYNTH-1'")]
    [InlineData("startswith(FullName, 'Synth')")]
    [InlineData("FullName eq")]
    [InlineData("NoSuchMember eq 'x'")]
    public async Task AFilterTheListCannotApply_Is400_NotA500(string filter)
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var controller = Controller(scope.ServiceProvider);

        var result = await controller.Get(QueryOptions(controller.HttpContext, "$filter=" + Uri.EscapeDataString(filter) + "&$top=10"));

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.DoesNotContain("Synth", System.Text.Json.JsonSerializer.Serialize(bad.Value));
        Assert.DoesNotContain("example", System.Text.Json.JsonSerializer.Serialize(bad.Value));
    }

    private static ServiceProvider Services()
    {
        var collection = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        collection.AddDbContext<ShiftDbContext, ListTestDbContext>(o => o.UseInMemoryDatabase(databaseName));
        collection.AddSingleton<IModelBuildingContributor>(new DarlasticModelBuildingContributor(schema: null));
        collection.AddSingleton(DispatchProxy.Create<ITypeAuthService, GoldenCustomerByVinTests.DeniesEverything>());
        collection.AddSingleton<IPiiMasker>(new DefaultPiiMasker(Options.Create(new PiiOptions())));
        collection.AddSingleton(Options.Create(new PiiOptions()));
        return collection.BuildServiceProvider();
    }

    private static GoldenCustomerController Controller(IServiceProvider services)
    {
        var protector = new PiiDtoProtector(new PiiFieldProtection(new DefaultPiiMasker(Options.Create(new PiiOptions()))));
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("localhost");
        http.Request.Path = "/api/Darlastic/GoldenCustomer";
        return new GoldenCustomerController(services.GetRequiredService<ShiftDbContext>(), Options.Create(new DarlasticApiOptions()), protector)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };
    }

    private static ODataQueryOptions<GoldenCustomerListDTO> QueryOptions(HttpContext http, string query)
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<GoldenCustomerListDTO>("GoldenCustomer");
        var model = builder.GetEdmModel();
        http.Request.QueryString = new QueryString("?" + query);
        var context = new ODataQueryContext(model, typeof(GoldenCustomerListDTO), new Microsoft.OData.UriParser.ODataPath());
        return new ODataQueryOptions<GoldenCustomerListDTO>(context, http.Request);
    }

    private sealed class ListTestDbContext(DbContextOptions<ListTestDbContext> options) : ShiftDbContext(options);
}
