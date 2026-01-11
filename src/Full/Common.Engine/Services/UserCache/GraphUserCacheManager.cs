using Common.Engine.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

using Common.Engine.Models;

using Common.Engine.Config;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// Manages user data caching with delta query synchronization and Copilot stats integration.
/// Provides efficient user data retrieval by caching Graph API results in Azure Table Storage.
/// </summary>
public class GraphUserCacheManager : GraphUserCacheManagerBase
{
    private readonly TableStorageManager _storageManager;
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphUserCacheManager> _logger;
    private readonly UserCacheConfig _config;

    private readonly DeltaQueryService _deltaQueryService;
    private readonly UserCacheStorageService _storageService;
    private readonly CopilotStatsService _copilotStatsService;

    private static readonly string[] UserSelectProperties =
    [
        "id",
        "userPrincipalName",
        "displayName",
        "givenName",
        "surname",
        "mail",
        "department",
        "jobTitle",
        "officeLocation",
        "city",
        "country",
        "state",
        "companyName",
        "employeeType",
        "employeeHireDate",
        "accountEnabled",
        "userType"
    ];

    public GraphUserCacheManager(
        string storageConnectionString,
        GraphServiceClient graphClient,
        ILogger<GraphUserCacheManager> logger,
        UserCacheConfig? config = null)
    {
        _storageManager = new ConcreteTableStorageManager(storageConnectionString);
        _graphClient = graphClient;
        _logger = logger;
        _config = config ?? new UserCacheConfig();

        // Initialize sub-services
        _deltaQueryService = new DeltaQueryService(graphClient, logger, UserSelectProperties);
        _storageService = new UserCacheStorageService(_storageManager, logger);
        _copilotStatsService = new CopilotStatsService(logger, _config);
    }

    #region Public Methods - Cache Operations

    /// <summary>
    /// Get all cached users. If cache is expired or empty, performs a sync.
    /// </summary>
    /// <param name="forceRefresh">Force a refresh regardless of cache state.</param>
    /// <returns>List of enriched user information from cache.</returns>
    public override async Task<List<EnrichedUserInfo>> GetAllCachedUsersAsync(bool forceRefresh = false)
    {
        var syncMetadata = await _storageService.GetSyncMetadataAsync();
        var needsSync = forceRefresh ||
                        syncMetadata.LastDeltaSyncDate == null ||
                        DateTime.UtcNow - syncMetadata.LastDeltaSyncDate.Value > _config.CacheExpiration;

        if (needsSync)
        {
            await SyncUsersAsync();
        }

        return await _storageService.GetAllUsersAsync();
    }

    /// <summary>
    /// Get a specific user from cache by UPN.
    /// </summary>
    /// <param name="upn">User Principal Name.</param>
    /// <returns>Enriched user information if found, null otherwise.</returns>
    public override async Task<EnrichedUserInfo?> GetCachedUserAsync(string upn)
    {
        return await _storageService.GetUserByUpnAsync(upn);
    }

    /// <summary>
    /// Clear all cached user data (useful for forcing a full resync).
    /// </summary>
    public override async Task ClearCacheAsync()
    {
        _logger.LogInformation("Clearing user cache...");

        var userCount = await _storageService.ClearAllUsersAsync();

        // Clear sync metadata
        var syncMetadata = await _storageService.GetSyncMetadataAsync();
        syncMetadata.DeltaLink = null;
        syncMetadata.LastFullSyncDate = null;
        syncMetadata.LastDeltaSyncDate = null;
        await _storageService.UpdateSyncMetadataAsync(syncMetadata);

        _logger.LogInformation($"Cleared {userCount} users from cache");
    }

    #endregion

    #region Public Methods - Synchronization

    /// <summary>
    /// Synchronize users from Microsoft Graph using delta query for incremental updates.
    /// </summary>
    public override async Task SyncUsersAsync()
    {
        var syncMetadata = await _storageService.GetSyncMetadataAsync();
        var needsFullSync = syncMetadata.LastFullSyncDate == null ||
                            DateTime.UtcNow - syncMetadata.LastFullSyncDate.Value > _config.FullSyncInterval;

        try
        {
            syncMetadata.LastSyncStatus = "InProgress";
            await _storageService.UpdateSyncMetadataAsync(syncMetadata);

            if (needsFullSync || string.IsNullOrEmpty(syncMetadata.DeltaLink))
            {
                await PerformFullSyncAsync(syncMetadata);
            }
            else
            {
                await PerformDeltaSyncAsync(syncMetadata);
            }

            syncMetadata.LastSyncStatus = "Success";
            syncMetadata.LastSyncError = null;
            await _storageService.UpdateSyncMetadataAsync(syncMetadata);

            _logger.LogInformation("User sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user sync");
            syncMetadata.LastSyncStatus = "Failed";
            syncMetadata.LastSyncError = ex.Message;
            await _storageService.UpdateSyncMetadataAsync(syncMetadata);
            throw;
        }
    }

    #endregion

    #region Public Methods - Copilot Statistics

    /// <summary>
    /// Update Copilot usage statistics for all cached users.
    /// </summary>
    public override async Task UpdateCopilotStatsAsync()
    {
        var syncMetadata = await _storageService.GetSyncMetadataAsync();

        // Check if we need to update stats
        if (syncMetadata.LastCopilotStatsUpdate != null &&
            DateTime.UtcNow - syncMetadata.LastCopilotStatsUpdate.Value < _config.CopilotStatsRefreshInterval)
        {
            _logger.LogInformation("Copilot stats are still fresh, skipping update");
            return;
        }

        _logger.LogInformation("Fetching Copilot usage statistics...");

        try
        {
            var stats = await _copilotStatsService.GetCopilotUsageStatsAsync();
            var tableClient = await _storageService.GetUserCacheTableClientAsync();
            await _copilotStatsService.UpdateCachedUsersWithStatsAsync(tableClient, stats);

            syncMetadata.LastCopilotStatsUpdate = DateTime.UtcNow;
            await _storageService.UpdateSyncMetadataAsync(syncMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Copilot stats");
            throw;
        }
    }

    #endregion

    #region Private Methods - Synchronization

    private async Task PerformFullSyncAsync(UserSyncMetadataEntity syncMetadata)
    {
        _logger.LogInformation("Performing full user sync with delta query initialization...");

        var tableClient = await _storageService.GetUserCacheTableClientAsync();
        var usersToProcess = await _deltaQueryService.FetchAllUsersAsync();

        // Process all users
        var userCount = 0;
        foreach (var user in usersToProcess.Users)
        {
            await _storageService.UpsertUserAsync(tableClient, user);
            userCount++;
        }

        // Update metadata with delta link
        syncMetadata.DeltaLink = usersToProcess.DeltaLink;
        syncMetadata.LastFullSyncDate = DateTime.UtcNow;
        syncMetadata.LastDeltaSyncDate = DateTime.UtcNow;
        syncMetadata.LastSyncUserCount = userCount;

        _logger.LogInformation($"Full sync completed: {userCount} users synchronized");
    }

    private async Task PerformDeltaSyncAsync(UserSyncMetadataEntity syncMetadata)
    {
        _logger.LogInformation("Performing delta sync...");

        if (string.IsNullOrEmpty(syncMetadata.DeltaLink))
        {
            _logger.LogWarning("No delta link available, performing full sync instead");
            await PerformFullSyncAsync(syncMetadata);
            return;
        }

        var tableClient = await _storageService.GetUserCacheTableClientAsync();
        var deltaChanges = await _deltaQueryService.FetchDeltaChangesAsync(syncMetadata.DeltaLink);

        // Process all changes
        var userCount = 0;
        foreach (var user in deltaChanges.Users)
        {
            await _storageService.UpsertUserAsync(tableClient, user);
            userCount++;
        }

        // Update metadata with new delta link
        syncMetadata.DeltaLink = deltaChanges.DeltaLink ?? syncMetadata.DeltaLink;
        syncMetadata.LastDeltaSyncDate = DateTime.UtcNow;
        syncMetadata.LastSyncUserCount = userCount;

        _logger.LogInformation($"Delta sync completed: {userCount} changes processed");
    }

    #endregion
}

