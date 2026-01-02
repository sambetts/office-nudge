using Common.Engine.Config;
using Entities.DB.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Engine.DependencyInjection;

/// <summary>
/// Extension methods for registering data access services
/// </summary>
public static class DataServiceExtensions
{
    /// <summary>
    /// Registers Entity Framework DbContext with SQL Server
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">Application configuration containing connection strings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, AppConfig config)
    {
        services.AddDbContext<DataContext>(options => options
            .UseSqlServer(config.ConnectionStrings.SQL)
        );

        return services;
    }
}
