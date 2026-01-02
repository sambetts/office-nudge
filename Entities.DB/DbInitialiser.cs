using Entities.DB.DbContexts;
using Microsoft.Extensions.Logging;

namespace Entities.DB;

public class DbInitialiser
{

    /// <summary>
    /// Ensure created and with base data
    /// </summary>
    public static async Task EnsureInitialised(DataContext context, ILogger logger, string? defaultUserUPN, bool insertDebug)
    {
        var createdNewDb = await context.Database.EnsureCreatedAsync();

        if (createdNewDb)
        {
            logger.LogInformation("Database created");


            if (insertDebug)
            {
                logger.LogInformation("Adding debugging test data");


                await context.SaveChangesAsync();
            }

        }
    }

}
