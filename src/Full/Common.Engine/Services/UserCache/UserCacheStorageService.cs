using Azure.Data.Tables;
using Common.Engine.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

using Common.Engine.Models;

using Common.Engine.Config;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// Handles storage operations for user cache in Azure Table Storage.
/// </summary>
internal class UserCacheStorageService
{
    private readonly TableStorageManager _storageManager;
    private readonly ILogger _logger;
    private const string USER_CACHE_TABLE = "usercache";
    private const string SYNC_METADATA_TABLE = "usersyncmetadata";

    public UserCacheStorageService(TableStorageManager storageManager, ILogger logger)
    {
        _storageManager = storageManager;
        _logger = logger;
    }

    /// <summary>
    /// Upsert a user to the cache.
    /// </summary>
    public async Task UpsertUserAsync(TableClient tableClient, User user)
    {
        var isDeleted = user.AdditionalData?.ContainsKey("@removed") == true;

        var cachedUser = new UserCacheTableEntity
        {
            RowKey = user.UserPrincipalName ?? user.Id!,
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
            EmployeeHireDate = user.EmployeeHireDate?.DateTime,
            LastSyncedDate = DateTime.UtcNow,
            IsDeleted = isDeleted
        };

        await tableClient.UpsertEntityAsync(cachedUser, TableUpdateMode.Replace);
    }

    /// <summary>
    /// Get all non-deleted users from cache.
    /// </summary>
    public async Task<List<EnrichedUserInfo>> GetAllUsersAsync()
    {
        var tableClient = await _storageManager.GetTableClient(USER_CACHE_TABLE);
        var users = new List<EnrichedUserInfo>();

        await foreach (var entity in tableClient.QueryAsync<UserCacheTableEntity>(
            filter: $"PartitionKey eq '{UserCacheTableEntity.PartitionKeyVal}' and IsDeleted eq false"))
        {
            users.Add(MapToEnrichedUser(entity));
        }

        return users;
    }

    /// <summary>
    /// Get a specific user by UPN.
    /// </summary>
    public async Task<EnrichedUserInfo?> GetUserByUpnAsync(string upn)
    {
        try
        {
            var tableClient = await _storageManager.GetTableClient(USER_CACHE_TABLE);
            var response = await tableClient.GetEntityAsync<UserCacheTableEntity>(
                UserCacheTableEntity.PartitionKeyVal,
                upn);

            if (response.Value != null && !response.Value.IsDeleted)
            {
                return MapToEnrichedUser(response.Value);
            }

            return null;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Clear all cached users.
    /// </summary>
    public async Task<int> ClearAllUsersAsync()
    {
        var tableClient = await _storageManager.GetTableClient(USER_CACHE_TABLE);
        var users = new List<UserCacheTableEntity>();

        await foreach (var user in tableClient.QueryAsync<UserCacheTableEntity>(
            filter: $"PartitionKey eq '{UserCacheTableEntity.PartitionKeyVal}'"))
        {
            users.Add(user);
        }

        foreach (var user in users)
        {
            await tableClient.DeleteEntityAsync(user.PartitionKey, user.RowKey);
        }

        return users.Count;
    }

    /// <summary>
    /// Get sync metadata.
    /// </summary>
    public async Task<UserSyncMetadataEntity> GetSyncMetadataAsync()
    {
        var tableClient = await _storageManager.GetTableClient(SYNC_METADATA_TABLE);

        try
        {
            var response = await tableClient.GetEntityAsync<UserSyncMetadataEntity>(
                UserSyncMetadataEntity.PartitionKeyVal,
                UserSyncMetadataEntity.SingletonRowKey);

            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return new UserSyncMetadataEntity();
        }
    }

    /// <summary>
    /// Update sync metadata.
    /// </summary>
    public async Task UpdateSyncMetadataAsync(UserSyncMetadataEntity metadata)
    {
        var tableClient = await _storageManager.GetTableClient(SYNC_METADATA_TABLE);
        await tableClient.UpsertEntityAsync(metadata, TableUpdateMode.Replace);
    }

    /// <summary>
    /// Get table client for user cache.
    /// </summary>
    public async Task<TableClient> GetUserCacheTableClientAsync()
    {
        return await _storageManager.GetTableClient(USER_CACHE_TABLE);
    }

    private static EnrichedUserInfo MapToEnrichedUser(UserCacheTableEntity entity)
    {
        return new EnrichedUserInfo
        {
            Id = entity.Id,
            UserPrincipalName = entity.UserPrincipalName,
            DisplayName = entity.DisplayName,
            GivenName = entity.GivenName,
            Surname = entity.Surname,
            Mail = entity.Mail,
            Department = entity.Department,
            JobTitle = entity.JobTitle,
            OfficeLocation = entity.OfficeLocation,
            City = entity.City,
            Country = entity.Country,
            State = entity.State,
            CompanyName = entity.CompanyName,
            EmployeeType = entity.EmployeeType,
            HireDate = entity.EmployeeHireDate.HasValue ? new DateTimeOffset(entity.EmployeeHireDate.Value) : null,
            ManagerUpn = entity.ManagerUpn,
            ManagerDisplayName = entity.ManagerDisplayName
        };
    }
}

