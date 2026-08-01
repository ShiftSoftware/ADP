using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ShiftSoftware.ADP.Menus.Sample.Functions;

// The sweep half of the menu → Cosmos replication. The sample API replicates on save (the trigger);
// this host re-syncs on a schedule, which is the only thing that can reach rows the trigger never saw
// — a catalogue that predates replication being switched on, or anything missed while Cosmos was
// unreachable. See ADP.Menus/COSMOS_REPLICATION_PLAN.md §17.

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder
        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true, reloadOnChange: true))
    .ConfigureServices((context, services) =>
    {
        // MenuReplicationDB maps the menu tables the same way the API host does — see its remarks.
        // It points at the same SQL database the sample API writes to.
        services.AddDbContext<MenuReplicationDB>(database =>
            database.UseSqlServer(context.Configuration.GetConnectionString("SQLServer")));

        // CosmosDBReplication resolves IMapper on construction, so AutoMapper has to be present even
        // though replication never uses it: every menu projection is an explicit manual delegate in
        // MenuCosmosMappers. An empty configuration is therefore exactly right — registering the
        // menus' AutoMapper profiles here would pull in DTO/hash-id services this host does not have.
        services.AddAutoMapper(_ => { });

        // Registers the CosmosDBReplication service the timers and the HTTP endpoint drive.
        services.AddShiftEntityCosmosDbReplication<MenuReplicationDB>();
    })
    .Build();

host.Run();
