using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Collections.Concurrent;

using Common.Engine.Models;

using Common.Engine.Config;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// In-memory implementation of user cache manager for development and testing.
/// Does not persist data between application restarts.
/// </summary>
public class InMemoryUserCacheManager : GraphUserCacheManagerBase
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger _logger;
    private readonly UserCacheConfig _config;
    private readonly DeltaQueryService _deltaQueryService;

    private readonly ConcurrentDictionary<string, EnrichedUserInfo> _userCache = new();
    private UserSyncMetadata _syncMetadata = new();

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

    public InMemoryUserCacheManager(
        GraphServiceClient graphClient,
        ILogger<InMemoryUserCacheManager> logger,
        UserCacheConfig? config = null)
    {
        _graphClient = graphClient;
        _logger = logger;
        _config = config ?? new UserCacheConfig();
        _deltaQueryService = new DeltaQueryService(graphClient, logger, UserSelectProperties);
    }

    /// <summary>
    /// Get all cached users. If cache is expired or empty, performs a sync.
    /// </summary>
    public override async Task<List<EnrichedUserInfo>> GetAllCachedUsersAsync(bool forceRefresh = false)
    {
        var needsSync = forceRefresh ||
                        _syncMetadata.LastDeltaSyncDate == null ||
                        DateTime.UtcNow - _syncMetadata.LastDeltaSyncDate.Value > _config.CacheExpiration;

        if (needsSync)
        {
            await SyncUsersAsync();
        }

        return _userCache.Values
            .Where(u => !u.IsDeleted)
            .ToList();
    }

    /// <summary>
    /// Get a specific user from cache by UPN.
    /// </summary>
    public override Task<EnrichedUserInfo?> GetCachedUserAsync(string upn)
    {
        if (_userCache.TryGetValue(upn, out var user) && !user.IsDeleted)
        {
            return Task.FromResult<EnrichedUserInfo?>(user);
        }

        return Task.FromResult<EnrichedUserInfo?>(null);
    }

    /// <summary>
    /// Clear all cached user data.
    /// </summary>
    public override Task ClearCacheAsync()
    {
        _logger.LogInformation("Clearing in-memory user cache...");

        var count = _userCache.Count;
        _userCache.Clear();

        _syncMetadata = new UserSyncMetadata
        {
            DeltaLink = null,
            LastFullSyncDate = null,
            LastDeltaSyncDate = null
        };

        _logger.LogInformation($"Cleared {count} users from in-memory cache");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronize users from Microsoft Graph using delta query for incremental updates.
    /// </summary>
    public override async Task SyncUsersAsync()
    {
        var needsFullSync = _syncMetadata.LastFullSyncDate == null ||
                            DateTime.UtcNow - _syncMetadata.LastFullSyncDate.Value > _config.FullSyncInterval;

        try
        {
            _syncMetadata.LastSyncStatus = "InProgress";

            if (needsFullSync || string.IsNullOrEmpty(_syncMetadata.DeltaLink))
            {
                await PerformFullSyncAsync();
            }
            else
            {
                await PerformDeltaSyncAsync();
            }

            _syncMetadata.LastSyncStatus = "Success";
            _syncMetadata.LastSyncError = null;

            _logger.LogInformation("User sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user sync");
            _syncMetadata.LastSyncStatus = "Failed";
            _syncMetadata.LastSyncError = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// Update Copilot usage statistics - not supported in in-memory implementation.
    /// </summary>
    public override Task UpdateCopilotStatsAsync()
    {
        _logger.LogWarning("Copilot stats updates are not supported in in-memory cache manager");
        return Task.CompletedTask;
    }

    #region Private Methods

    private async Task PerformFullSyncAsync()
    {
        _logger.LogInformation("Performing full user sync with delta query initialization...");

        var usersToProcess = await _deltaQueryService.FetchAllUsersAsync();

        var userCount = 0;
        foreach (var user in usersToProcess.Users)
        {
            UpsertUser(user);
            userCount++;
        }

        _syncMetadata.DeltaLink = usersToProcess.DeltaLink;
        _syncMetadata.LastFullSyncDate = DateTime.UtcNow;
        _syncMetadata.LastDeltaSyncDate = DateTime.UtcNow;
        _syncMetadata.LastSyncUserCount = userCount;

        _logger.LogInformation($"Full sync completed: {userCount} users synchronized");
    }

    private async Task PerformDeltaSyncAsync()
    {
        _logger.LogInformation("Performing delta sync...");

        if (string.IsNullOrEmpty(_syncMetadata.DeltaLink))
        {
            _logger.LogWarning("No delta link available, performing full sync instead");
            await PerformFullSyncAsync();
            return;
        }

        var deltaChanges = await _deltaQueryService.FetchDeltaChangesAsync(_syncMetadata.DeltaLink);

        var userCount = 0;
        foreach (var user in deltaChanges.Users)
        {
            UpsertUser(user);
            userCount++;
        }

        _syncMetadata.DeltaLink = deltaChanges.DeltaLink ?? _syncMetadata.DeltaLink;
        _syncMetadata.LastDeltaSyncDate = DateTime.UtcNow;
        _syncMetadata.LastSyncUserCount = userCount;

        _logger.LogInformation($"Delta sync completed: {userCount} changes processed");
    }

    private void UpsertUser(Microsoft.Graph.Models.User user)
    {
        var isDeleted = user.AdditionalData?.ContainsKey("@removed") == true;
        var upn = user.UserPrincipalName ?? user.Id!;

        var enrichedUser = new EnrichedUserInfo
        {
            Id = user.Id ?? string.Empty,
            UserPrincipalName = user.UserPrincipalName ?? string.Empty,
            DisplayName = user.DisplayName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            Mail = user.Mail,
            Department = user.Department,
            JobTitle = user.JobTitle,
            OfficeLocation = user.OfficeLocation,
            City = user.City,
            Country = user.Country,
            State = user.State,
            CompanyName = user.CompanyName,
            EmployeeType = user.EmployeeType,
            HireDate = user.EmployeeHireDate,
            IsDeleted = isDeleted
        };

        _userCache.AddOrUpdate(upn, enrichedUser, (key, existing) => enrichedUser);
    }

    #endregion

    /// <summary>
    /// In-memory metadata structure.
    /// </summary>
    private class UserSyncMetadata
    {
        public string? DeltaLink { get; set; }
        public DateTime? LastFullSyncDate { get; set; }
        public DateTime? LastDeltaSyncDate { get; set; }
        public DateTime? LastCopilotStatsUpdate { get; set; }
        public string? LastSyncStatus { get; set; }
        public string? LastSyncError { get; set; }
        public int LastSyncUserCount { get; set; }
    }
}

