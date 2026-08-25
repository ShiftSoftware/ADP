using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Menus.Sample.Functions;
using ShiftSoftware.ADP.Menus.Sync.Extensions;

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
    // EF Core prints every SQL statement at Information, which drowns the replication and DuckDB-sync
    // narration this host exists to demonstrate. Warnings and errors still come through; lower this
    // back to Information when the queries themselves are what you are debugging.
    .ConfigureLogging(logging =>
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning))
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

        // The SQL → DuckDB half: the stateless sync service POST api/duckdb-sync drives, table by
        // table, into a local DuckDB file. The DbContext and the write connection are passed per call.
        services.AddServiceMenuDuckDBSync();

        // ---------- the READ side ----------
        // The lookup turns a basic model code back into menu codes and prices, out of the very
        // documents the sweep above writes — so this host is a full round trip.

        // Registered UNCONDITIONALLY, unlike the replication timers. Those fire on a schedule, so an
        // unconfigured host would log an error every hour for something nobody asked for; the lookup
        // only runs when someone calls it, and then a missing connection string is a fault worth
        // hearing about. Gating this instead would mean either a service the function cannot inject, or
        // a cheerful 200 saying "no menu" — which is exactly the misconfiguration-hiding answer the
        // lookup already refuses to give for an unprovisioned container.
        //
        // The factory defers reading the connection string until something actually needs Cosmos, and
        // says what to do about it rather than failing as an opaque DI error.
        services.AddSingleton(provider =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("Cosmos");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:Cosmos is not set, so the service-menu lookup cannot read. Set it in "
                    + "local.settings.json (the Azure Cosmos DB Emulator's endpoint and key are already there "
                    + "by default) and restart the host.");

            return new CosmosClient(connectionString);
        });

        // AddLookupService brings the vehicle lookup AND the service-menu lookup, because the menu is part
        // of the vehicle lookup result. That is what lets this host serve both reads: GET api/menu/{code}
        // straight off ServiceMenuLookupService, and GET api/vehicle/{vin} through VehicleLookupService
        // with the menu attached — the join the menu lookup alone cannot demonstrate.
        //
        // A host that wants menus WITHOUT the vehicle lookup calls AddServiceMenuLookup(…) instead; it takes
        // the same options action and registers only the menu half.
        services.AddLookupService(options =>
        {
            // The vehicle half reads CompanyData/Vehicles out of Cosmos. Required — without it
            // IVehicleLookupStorageService is never registered and VehicleLookupService cannot resolve.
            // Note this sample does not PROVISION those containers; see VehicleMenuLookupFunctions.
            options.VehicleLookupStorageSource = StorageSources.CosmosDB;

            // The menu half's own options, reached from this one call so a host cannot end up with the
            // menu lookup registered and silently running on defaults.
            options.ConfigureServiceMenu = menu =>
            {
                // The sample's data is authored without country-specific prices, so country 0 is what its
                // part prices are stored under. A multi-country deployment sets this to its own id and wires
                // ServiceMenuLookupOptions.CountrySettingsResolver.
                menu.DefaultCountryID = 0;
            };
        });
    })
    .Build();

host.Run();
