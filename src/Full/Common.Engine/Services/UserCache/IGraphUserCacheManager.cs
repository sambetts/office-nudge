using Common.Engine.Config;

using Common.Engine.Models;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// Abstract base class for user cache management with Graph API synchronization.
/// </summary>
public abstract class GraphUserCacheManagerBase
{
    /// <summary>
    /// Get all cached users. If cache is expired or empty, performs a sync.
    /// </summary>
    /// <param name="forceRefresh">Force a refresh regardless of cache state.</param>
    /// <returns>List of enriched user information from cache.</returns>
    public abstract Task<List<EnrichedUserInfo>> GetAllCachedUsersAsync(bool forceRefresh = false);

    /// <summary>
    /// Get a specific user from cache by UPN.
    /// </summary>
    /// <param name="upn">User Principal Name.</param>
    /// <returns>Enriched user information if found, null otherwise.</returns>
    public abstract Task<EnrichedUserInfo?> GetCachedUserAsync(string upn);

    /// <summary>
    /// Clear all cached user data (useful for forcing a full resync).
    /// </summary>
    public abstract Task ClearCacheAsync();

    /// <summary>
    /// Synchronize users from Microsoft Graph using delta query for incremental updates.
    /// </summary>
    public abstract Task SyncUsersAsync();

    /// <summary>
    /// Update Copilot usage statistics for all cached users.
    /// </summary>
    public abstract Task UpdateCopilotStatsAsync();
}

