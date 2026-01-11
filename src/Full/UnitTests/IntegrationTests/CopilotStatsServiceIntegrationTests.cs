using Azure.Data.Tables;
using Common.Engine.Config;
using Common.Engine.Models;
using Common.Engine.Services.UserCache;
using Common.Engine.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.IntegrationTests;

/// <summary>
/// Integration tests for CopilotStatsService.
/// NOTE: These tests require actual Microsoft Graph connectivity and Reports.Read.All permission.
/// Some tests may be inconclusive if the tenant doesn't have Copilot licenses or usage data.
/// </summary>
[TestClass]
public class CopilotStatsServiceIntegrationTests : AbstractTest
{
    private CopilotStatsService? _service;
    private TableServiceClient? _tableServiceClient;
    private string _testTableName = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _testTableName = $"copilotteststats{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            var cacheConfig = new UserCacheConfig
            {
                CopilotStatsPeriod = "D30"
            };

            var statsLoader = new GraphCopilotStatsLoader(
                GetLogger<GraphCopilotStatsLoader>(),
                cacheConfig,
                _config.GraphConfig);

            _service = new CopilotStatsService(
                GetLogger<CopilotStatsService>(),
                statsLoader);

            _tableServiceClient = new TableServiceClient(_config.ConnectionStrings.Storage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to initialize CopilotStatsService: {ex.Message}");
            _logger.LogWarning("Tests will be skipped if Graph credentials or Reports.Read.All permission are not configured");
        }
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_tableServiceClient != null)
        {
            try
            {
                await _tableServiceClient.DeleteTableAsync(_testTableName);
                _logger.LogInformation($"Cleaned up test table: {_testTableName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error during cleanup: {ex.Message}");
            }
        }
    }

    #region Token Acquisition Tests

    [TestMethod]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsToken()
    {
        if (_service == null)
        {
            Assert.Inconclusive("Service not initialized - check Graph API credentials");
            return;
        }

        // Act & Assert - The service should get a token when calling the API
        try
        {
            var stats = await _service.GetCopilotUsageStatsAsync();
            
            // If we got here without exception, token acquisition worked
            Assert.IsNotNull(stats);
            _logger.LogInformation("Successfully acquired access token and retrieved Copilot stats");
        }
        catch (Exception ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Assert.Inconclusive("Reports.Read.All permission not granted or tenant doesn't have Copilot licenses");
        }
        catch (Exception ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            Assert.Fail($"Authentication failed: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_CachesToken_ForMultipleCalls()
    {
        if (_service == null)
        {
            Assert.Inconclusive("Service not initialized - check Graph API credentials");
            return;
        }

        try
        {
            // Act - Make multiple calls that will use the same token
            var startTime = DateTime.UtcNow;
            
            var stats1 = await _service.GetCopilotUsageStatsAsync();
            var firstCallTime = DateTime.UtcNow - startTime;
            
            startTime = DateTime.UtcNow;
            var stats2 = await _service.GetCopilotUsageStatsAsync();
            var secondCallTime = DateTime.UtcNow - startTime;

            // Assert - Second call should be faster or similar (using cached token)
            Assert.IsNotNull(stats1);
            Assert.IsNotNull(stats2);
            
            _logger.LogInformation($"First call: {firstCallTime.TotalMilliseconds}ms, Second call: {secondCallTime.TotalMilliseconds}ms");
            
            // Token caching should make subsequent calls faster or at least not significantly slower
            Assert.IsTrue(secondCallTime.TotalMilliseconds < firstCallTime.TotalMilliseconds * 2,
                "Second call should benefit from cached token");
        }
        catch (Exception ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Assert.Inconclusive("Reports.Read.All permission not granted or tenant doesn't have Copilot licenses");
        }
    }

    #endregion

    #region Copilot Stats Retrieval Tests

    [TestMethod]
    public async Task GetCopilotUsageStatsAsync_ReturnsRecords()
    {
        if (_service == null)
        {
            Assert.Inconclusive("Service not initialized - check Graph API credentials");
            return;
        }

        try
        {
            // Act
            var result = await _service.GetCopilotUsageStatsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success, "Stats retrieval should be successful");
            _logger.LogInformation($"Retrieved {result.Records.Count} Copilot usage records (Status: {result.StatusCode})");

            if (!result.Success)
            {
                _logger.LogWarning($"Copilot stats retrieval failed: {result.ErrorMessage}");
                Assert.Inconclusive($"Copilot stats unavailable: {result.ErrorMessage}");
                return;
            }

            if (result.Records.Count == 0)
            {
                _logger.LogWarning("No Copilot usage data found - tenant may not have active Copilot users");
                Assert.Inconclusive("No Copilot usage data available in tenant");
                return;
            }

            // Verify structure of first record
            var firstRecord = result.Records[0];
            Assert.IsFalse(string.IsNullOrEmpty(firstRecord.UserPrincipalName), "Record should have UserPrincipalName");
            
            _logger.LogInformation($"Sample record: {firstRecord.UserPrincipalName}");
            _logger.LogInformation($"  Last Activity: {firstRecord.LastActivityDate}");
            _logger.LogInformation($"  Copilot Chat: {firstRecord.CopilotChatLastActivityDate}");
            _logger.LogInformation($"  Teams Copilot: {firstRecord.TeamsCopilotLastActivityDate}");
        }
        catch (Exception ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Assert.Inconclusive("Reports.Read.All permission not granted or tenant doesn't have Copilot licenses");
        }
    }

    [TestMethod]
    public async Task GetCopilotUsageStatsAsync_ParsesAllActivityDates()
    {
        if (_service == null)
        {
            Assert.Inconclusive("Service not initialized - check Graph API credentials");
            return;
        }

        try
        {
            // Act
            var result = await _service.GetCopilotUsageStatsAsync();

            if (!result.Success || result.Records.Count == 0)
            {
                Assert.Inconclusive("No Copilot usage data available in tenant");
                return;
            }

            // Assert - Check that we can parse various activity types
            var recordsWithActivity = result.Records.Where(r => r.LastActivityDate.HasValue).ToList();
            
            _logger.LogInformation($"Records with any activity: {recordsWithActivity.Count}/{result.Records.Count}");
            
            if (recordsWithActivity.Count > 0)
            {
                var sampleRecord = recordsWithActivity[0];
                var activityTypes = new Dictionary<string, DateTime?>
                {
                    { "Overall", sampleRecord.LastActivityDate },
                    { "Copilot Chat", sampleRecord.CopilotChatLastActivityDate },
                    { "Teams", sampleRecord.TeamsCopilotLastActivityDate },
                    { "Word", sampleRecord.WordCopilotLastActivityDate },
                    { "Excel", sampleRecord.ExcelCopilotLastActivityDate },
                    { "PowerPoint", sampleRecord.PowerPointCopilotLastActivityDate },
                    { "Outlook", sampleRecord.OutlookCopilotLastActivityDate },
                    { "OneNote", sampleRecord.OneNoteCopilotLastActivityDate },
                    { "Loop", sampleRecord.LoopCopilotLastActivityDate }
                };

                foreach (var activity in activityTypes.Where(a => a.Value.HasValue))
                {
                    _logger.LogInformation($"  {activity.Key}: {activity.Value}");
                }

                Assert.IsTrue(activityTypes.Values.Any(v => v.HasValue), 
                    "At least one activity type should have a date");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Assert.Inconclusive("Reports.Read.All permission not granted or tenant doesn't have Copilot licenses");
        }
    }

    [TestMethod]
    public async Task GetCopilotUsageStatsAsync_HandlesNoData_Gracefully()
    {
        if (_service == null)
        {
            Assert.Inconclusive("Service not initialized - check Graph API credentials");
            return;
        }

        try
        {
            // Act
            var result = await _service.GetCopilotUsageStatsAsync();

            // Assert - Should return empty list, not null or throw
            Assert.IsNotNull(result);
            
            if (result.Records.Count == 0)
            {
                _logger.LogInformation("No Copilot usage data found - this is acceptable for tenants without active Copilot users");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Assert.Inconclusive("Reports.Read.All permission not granted or tenant doesn't have Copilot licenses");
        }
    }

    #endregion

    #region Update Cached Users Tests

    [TestMethod]
    public async Task UpdateCachedUsersWithStatsAsync_UpdatesExistingUsers()
    {
        if (_service == null || _tableServiceClient == null)
        {
            Assert.Inconclusive("Service not initialized - check configuration");
            return;
        }

        // Arrange - Create test table and add test user
        var tableClient = _tableServiceClient.GetTableClient(_testTableName);
        await tableClient.CreateIfNotExistsAsync();

        var testUser = new UserCacheTableEntity
        {
            PartitionKey = UserCacheTableEntity.PartitionKeyVal,
            RowKey = "testuser@contoso.com",
            UserPrincipalName = "testuser@contoso.com",
            DisplayName = "Test User"
        };
        await tableClient.AddEntityAsync(testUser);

        var stats = new List<CopilotUsageRecord>
        {
            new CopilotUsageRecord
            {
                UserPrincipalName = "testuser@contoso.com",
                LastActivityDate = DateTime.UtcNow.AddDays(-1),
                CopilotChatLastActivityDate = DateTime.UtcNow.AddDays(-2),
                TeamsCopilotLastActivityDate = DateTime.UtcNow.AddDays(-3)
            }
        };

        // Act
        await _service.UpdateCachedUsersWithStatsAsync(tableClient, stats);

        // Assert
        var updatedUser = await tableClient.GetEntityAsync<UserCacheTableEntity>(
            UserCacheTableEntity.PartitionKeyVal, "testuser@contoso.com");
        
        Assert.IsNotNull(updatedUser.Value.CopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.CopilotChatLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.TeamscopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.LastCopilotStatsUpdate);

        _logger.LogInformation($"Successfully updated user with Copilot stats");
        _logger.LogInformation($"  Last Activity: {updatedUser.Value.CopilotLastActivityDate}");
        _logger.LogInformation($"  Last Stats Update: {updatedUser.Value.LastCopilotStatsUpdate}");
    }

    [TestMethod]
    public async Task UpdateCachedUsersWithStatsAsync_SkipsNonExistentUsers()
    {
        if (_service == null || _tableServiceClient == null)
        {
            Assert.Inconclusive("Service not initialized - check configuration");
            return;
        }

        // Arrange - Create empty test table
        var tableClient = _tableServiceClient.GetTableClient(_testTableName);
        await tableClient.CreateIfNotExistsAsync();

        var stats = new List<CopilotUsageRecord>
        {
            new CopilotUsageRecord
            {
                UserPrincipalName = "nonexistent@contoso.com",
                LastActivityDate = DateTime.UtcNow
            }
        };

        // Act - Should not throw
        await _service.UpdateCachedUsersWithStatsAsync(tableClient, stats);

        // Assert - Table should still be empty
        var entities = tableClient.QueryAsync<UserCacheTableEntity>();
        var count = 0;
        await foreach (var entity in entities)
        {
            count++;
        }

        Assert.AreEqual(0, count, "Should not create new users, only update existing ones");
        _logger.LogInformation("Correctly skipped non-existent user");
    }

    [TestMethod]
    public async Task UpdateCachedUsersWithStatsAsync_UpdatesMultipleUsers()
    {
        if (_service == null || _tableServiceClient == null)
        {
            Assert.Inconclusive("Service not initialized - check configuration");
            return;
        }

        // Arrange - Create test table with multiple users
        var tableClient = _tableServiceClient.GetTableClient(_testTableName);
        await tableClient.CreateIfNotExistsAsync();

        var users = new[]
        {
            new UserCacheTableEntity
            {
                PartitionKey = UserCacheTableEntity.PartitionKeyVal,
                RowKey = "user1@contoso.com",
                UserPrincipalName = "user1@contoso.com"
            },
            new UserCacheTableEntity
            {
                PartitionKey = UserCacheTableEntity.PartitionKeyVal,
                RowKey = "user2@contoso.com",
                UserPrincipalName = "user2@contoso.com"
            },
            new UserCacheTableEntity
            {
                PartitionKey = UserCacheTableEntity.PartitionKeyVal,
                RowKey = "user3@contoso.com",
                UserPrincipalName = "user3@contoso.com"
            }
        };

        foreach (var user in users)
        {
            await tableClient.AddEntityAsync(user);
        }

        var stats = new List<CopilotUsageRecord>
        {
            new CopilotUsageRecord { UserPrincipalName = "user1@contoso.com", LastActivityDate = DateTime.UtcNow.AddDays(-1) },
            new CopilotUsageRecord { UserPrincipalName = "user2@contoso.com", LastActivityDate = DateTime.UtcNow.AddDays(-2) },
            new CopilotUsageRecord { UserPrincipalName = "user3@contoso.com", LastActivityDate = DateTime.UtcNow.AddDays(-3) }
        };

        // Act
        await _service.UpdateCachedUsersWithStatsAsync(tableClient, stats);

        // Assert - All users should be updated
        foreach (var stat in stats)
        {
            var updatedUser = await tableClient.GetEntityAsync<UserCacheTableEntity>(
                UserCacheTableEntity.PartitionKeyVal, stat.UserPrincipalName);
            
            Assert.IsNotNull(updatedUser.Value.CopilotLastActivityDate);
            Assert.IsNotNull(updatedUser.Value.LastCopilotStatsUpdate);
        }

        _logger.LogInformation($"Successfully updated {stats.Count} users with Copilot stats");
    }

    [TestMethod]
    public async Task UpdateCachedUsersWithStatsAsync_UpdatesAllCopilotActivityTypes()
    {
        if (_service == null || _tableServiceClient == null)
        {
            Assert.Inconclusive("Service not initialized - check configuration");
            return;
        }

        // Arrange
        var tableClient = _tableServiceClient.GetTableClient(_testTableName);
        await tableClient.CreateIfNotExistsAsync();

        var testUser = new UserCacheTableEntity
        {
            PartitionKey = UserCacheTableEntity.PartitionKeyVal,
            RowKey = "testuser@contoso.com",
            UserPrincipalName = "testuser@contoso.com"
        };
        await tableClient.AddEntityAsync(testUser);

        var stats = new List<CopilotUsageRecord>
        {
            new CopilotUsageRecord
            {
                UserPrincipalName = "testuser@contoso.com",
                LastActivityDate = DateTime.UtcNow.AddDays(-1),
                CopilotChatLastActivityDate = DateTime.UtcNow.AddDays(-2),
                TeamsCopilotLastActivityDate = DateTime.UtcNow.AddDays(-3),
                WordCopilotLastActivityDate = DateTime.UtcNow.AddDays(-4),
                ExcelCopilotLastActivityDate = DateTime.UtcNow.AddDays(-5),
                PowerPointCopilotLastActivityDate = DateTime.UtcNow.AddDays(-6),
                OutlookCopilotLastActivityDate = DateTime.UtcNow.AddDays(-7),
                OneNoteCopilotLastActivityDate = DateTime.UtcNow.AddDays(-8),
                LoopCopilotLastActivityDate = DateTime.UtcNow.AddDays(-9)
            }
        };

        // Act
        await _service.UpdateCachedUsersWithStatsAsync(tableClient, stats);

        // Assert
        var updatedUser = await tableClient.GetEntityAsync<UserCacheTableEntity>(
            UserCacheTableEntity.PartitionKeyVal, "testuser@contoso.com");
        
        Assert.IsNotNull(updatedUser.Value.CopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.CopilotChatLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.TeamscopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.WordCopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.ExcelCopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.PowerPointCopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.OutlookCopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.OneNoteCopilotLastActivityDate);
        Assert.IsNotNull(updatedUser.Value.LoopCopilotLastActivityDate);

        _logger.LogInformation("Successfully updated all Copilot activity types");
    }

    #endregion

    #region Configuration Tests

    [TestMethod]
    public void CopilotStatsService_UsesDifferentPeriods_Correctly()
    {
        // Arrange & Act - Create services with different periods
        var periods = new[] { "D7", "D30", "D90", "D180" };
        var services = new List<CopilotStatsService>();

        foreach (var period in periods)
        {
            var config = new UserCacheConfig { CopilotStatsPeriod = period };
            var loader = new GraphCopilotStatsLoader(
                GetLogger<GraphCopilotStatsLoader>(),
                config,
                _config.GraphConfig);
            var service = new CopilotStatsService(
                GetLogger<CopilotStatsService>(),
                loader);
            services.Add(service);
        }

        // Assert - Services should be created with different configs
        Assert.AreEqual(periods.Length, services.Count);
        _logger.LogInformation($"Successfully created services for periods: {string.Join(", ", periods)}");
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task GetCopilotUsageStatsAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange - Create loader with invalid credentials
        var invalidConfig = new AzureADAuthConfig
        {
            TenantId = "invalid-tenant-id",
            ClientId = "invalid-client-id",
            ClientSecret = "invalid-secret"
        };

        var loader = new GraphCopilotStatsLoader(
            GetLogger<GraphCopilotStatsLoader>(),
            new UserCacheConfig(),
            invalidConfig);
        
        var service = new CopilotStatsService(
            GetLogger<CopilotStatsService>(),
            loader);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Azure.Identity.AuthenticationFailedException>(
            async () => await service.GetCopilotUsageStatsAsync(),
            "Should throw exception with invalid credentials");
    }

    [TestMethod]
    public async Task UpdateCachedUsersWithStatsAsync_WithEmptyStatsList_CompletesSuccessfully()
    {
        if (_service == null || _tableServiceClient == null)
        {
            Assert.Inconclusive("Service not initialized - check configuration");
            return;
        }

        // Arrange
        var tableClient = _tableServiceClient.GetTableClient(_testTableName);
        await tableClient.CreateIfNotExistsAsync();

        var emptyStats = new List<CopilotUsageRecord>();

        // Act - Should not throw
        await _service.UpdateCachedUsersWithStatsAsync(tableClient, emptyStats);

        // Assert
        _logger.LogInformation("Successfully handled empty stats list");
    }

    #endregion
}
