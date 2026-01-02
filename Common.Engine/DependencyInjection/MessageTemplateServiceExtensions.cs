using Common.Engine.Config;
using Common.Engine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Engine.DependencyInjection;

/// <summary>
/// Extension methods for registering message template services
/// </summary>
public static class MessageTemplateServiceExtensions
{
    /// <summary>
    /// Registers message template storage and service components
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">Application configuration containing connection strings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageTemplateServices(this IServiceCollection services, AppConfig config)
    {
        services.AddSingleton<MessageTemplateStorageManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MessageTemplateStorageManager>>();
            return new MessageTemplateStorageManager(config.ConnectionStrings.Storage, logger);
        });

        services.AddScoped<MessageTemplateService>();

        return services;
    }
}
