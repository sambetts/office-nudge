using Common.Engine;
using Entities.DB;
using Entities.DB.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(c =>
    {
#if DEBUG
        c.AddEnvironmentVariables()
            .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .Build();
#else
        c.AddEnvironmentVariables()
            .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)            
            .AddEnvironmentVariables()
            .Build();
#endif

    })
    .ConfigureServices((context, services) =>
    {
        var config = services.AddTeamsAppServices(context.Configuration);


        // UsageStatsReport
        services.AddDbContext<DataContext>(options => options.UseSqlServer(config.ConnectionStrings.SQL));

#if !DEBUG
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
#endif

        // Ensure DB
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlServer(config.ConnectionStrings.SQL);


        using (var db = new DataContext(optionsBuilder.Options))
        {
            var logger = LoggerFactory.Create(config =>
            {
                config.AddConsole();
            }).CreateLogger("Functions app");
            logger.LogTrace($"Ensuring DB is created and seeded using '{config.ConnectionStrings.SQL}'");
            DbInitialiser.EnsureInitialised(db, logger, config.TestUPN, config.DevMode).Wait();

            logger.LogTrace("Running host");
        }
    })
    .Build();

host.Run();
