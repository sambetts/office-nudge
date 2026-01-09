namespace Common.Engine.Config;

/// <summary>
/// Configuration for user cache and synchronization behavior.
/// </summary>
public class UserCacheConfig
{
    /// <summary>
    /// How long cached user data is valid before requiring a delta sync (default: 1 hour).
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How often to refresh Copilot usage stats (default: 24 hours).
    /// </summary>
    public TimeSpan CopilotStatsRefreshInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// How often to force a full sync instead of delta (default: 7 days).
    /// </summary>
    public TimeSpan FullSyncInterval { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Period for Copilot stats: D7, D30, D90, D180 (default: D30).
    /// </summary>
    public string CopilotStatsPeriod { get; set; } = "D30";
}
