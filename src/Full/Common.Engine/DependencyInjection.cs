using Common.Engine.Config;
using Common.Engine.DependencyInjection;
using Common.Engine.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Engine;

/// <summary>
/// Main dependency injection configuration for Common.Engine services.
/// Orchestrates registration of bot, graph, and data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required for a bot application including bot framework,
    /// Microsoft Graph, and data access services.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The configured BotConfig instance</returns>
    public static BotConfig AddBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new BotConfig(configuration);
        services.AddSingleton(config);
        services.AddSingleton<AppConfig>(config);
        services.AddSingleton<TeamsAppConfig>(config);

        // Register services using extension methods for better organization
        services.AddBotFrameworkServices();
        services.AddBotNotificationServices();
        services.AddGraphServices(config);
        services.AddMessageTemplateServices(config);
        services.AddStatisticsServices(config);
        services.AddSmartGroupServices(config);

        return config;
    }

    /// <summary>
    /// Registers services required for a Teams application (without bot framework).
    /// Includes Microsoft Graph and data access services.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The configured TeamsAppConfig instance</returns>
    public static TeamsAppConfig AddTeamsAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new TeamsAppConfig(configuration);
        services.AddSingleton(config);
        services.AddSingleton<AppConfig>(config);

        // Teams apps may not need full bot services
        services.AddGraphServices(config);
        services.AddMessageTemplateServices(config);
        services.AddStatisticsServices(config);
        services.AddSmartGroupServices(config);

        return config;
    }

    /// <summary>
    /// Registers statistics services including GraphService and StatisticsService
    /// </summary>
    private static IServiceCollection AddStatisticsServices(this IServiceCollection services, AppConfig config)
    {
        services.AddSingleton<GraphService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GraphService>>();
            return new GraphService(config.GraphConfig, logger);
        });

        services.AddScoped<StatisticsService>();

        return services;
    }

    /// <summary>
    /// Registers smart group services for Copilot Connected mode.
    /// Only fully functional when AIFoundryConfig is provided.
    /// </summary>
    private static IServiceCollection AddSmartGroupServices(this IServiceCollection services, TeamsAppConfig config)
    {
        // Register GraphUserService for enriched user metadata
        services.AddSingleton<GraphUserService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GraphUserService>>();
            return new GraphUserService(config.GraphConfig, logger);
        });

        // Register SmartGroupStorageManager
        services.AddSingleton<SmartGroupStorageManager>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmartGroupStorageManager>>();
            return new SmartGroupStorageManager(config.ConnectionStrings.Storage, logger);
        });

        // Register SettingsStorageManager
        services.AddSingleton<SettingsStorageManager>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SettingsStorageManager>>();
            return new SettingsStorageManager(config.ConnectionStrings.Storage, logger);
        });

        // Register AIFoundryService only if configured
        if (config.AIFoundryConfig != null)
        {
            services.AddSingleton<AIFoundryService>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AIFoundryService>>();
                var settingsManager = sp.GetRequiredService<SettingsStorageManager>();
                return new AIFoundryService(config.AIFoundryConfig, logger, settingsManager);
            });
        }

        // Register SmartGroupService
        services.AddScoped<SmartGroupService>(sp =>
        {
            var storageManager = sp.GetRequiredService<SmartGroupStorageManager>();
            var graphUserService = sp.GetRequiredService<GraphUserService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmartGroupService>>();
            var aiFoundryService = sp.GetService<AIFoundryService>(); // Optional - may be null
            return new SmartGroupService(storageManager, graphUserService, logger, aiFoundryService);
        });

        return services;
    }
}
