# User Cache Architecture Refactoring Plan

## Current Problems

1. **Mixed Responsibilities**: `GraphUserCacheManager` and `InMemoryUserCacheManager` both contain:
   - Graph API data loading logic
   - Delta query management
   - Cache persistence logic
   - Copilot stats logic

2. **Tight Coupling**: Cannot use fake/mock data loaders for testing
3. **Confusing Names**: "GraphUserCacheManager" implies it's tied to Graph
4. **No Clean Separation**: Storage and data loading are mixed

## Proposed Architecture

### New Structure

```
UserCacheManager (Concrete manager - NO base class needed)
  ??> IUserDataLoader (Adapter for loading users)
  ?    ?? GraphUserDataLoader (Production - Graph API)
  ?    ?? FakeUserDataLoader (Testing - Mock data)
  ?
  ??> ICacheStorage (Adapter for persisting cache)
  ?    ?? AzureTableCacheStorage (Production - Azure Tables)
  ?    ?? InMemoryCacheStorage (Testing/Dev - Memory)
  ?
  ??> CopilotStatsService (Optional stats provider)
```

### Benefits

? **Single Concrete Manager**: One `UserCacheManager` class, not multiple implementations  
? **Pluggable Data Loaders**: Easy to swap Graph API for fakes/mocks  
? **Pluggable Storage**: Easy to swap Azure Tables for in-memory/Redis/etc  
? **Clear Separation**: Data loading vs. persistence vs. orchestration  
? **Easy Testing**: Mock either adapter independently  
? **Production Clarity**: Always use `GraphUserDataLoader` + `AzureTableCacheStorage` for web  

## File Changes

### Files to Create

| File | Purpose | Lines |
|------|---------|-------|
| `IUserDataLoader.cs` | Interface for loading user data | ~30 |
| `ICacheStorage.cs` | Interface for cache persistence | ~40 |
| `GraphUserDataLoader.cs` | Graph API implementation of IUserDataLoader | ~200 |
| `FakeUserDataLoader.cs` | Test/mock implementation of IUserDataLoader | ~80 |
| `AzureTableCacheStorage.cs` | Azure Tables implementation of ICacheStorage | ~250 |
| `InMemoryCacheStorage.cs` | In-memory implementation of ICacheStorage | ~100 |
| `UserCacheManager.cs` | Single concrete manager using adapters | ~200 |

### Files to Modify

| File | Changes |
|------|---------|
| `IGraphUserCacheManager.cs` | **DELETE** - No longer needed |
| `GraphUserCacheManager.cs` | **DELETE** - Replaced by new architecture |
| `InMemoryUserCacheManager.cs` | **DELETE** - Replaced by adapters |
| `UserCacheStorageService.cs` | **REFACTOR** ? `AzureTableCacheStorage.cs` |
| `DeltaQueryService.cs` | **MOVE** ? Part of `GraphUserDataLoader.cs` |
| `CopilotStatsService.cs` | **KEEP** - Still used by data loader |
| `DependencyInjection.cs` | **UPDATE** - Register new adapters |
| `UserCacheController.cs` | **UPDATE** - Use new interface |
| `GraphUserService.cs` | **UPDATE** - Use new interface |

### Files to Update (Tests)

| File | Changes |
|------|---------|
| `GraphUserCacheManagerIntegrationTests.cs` | **UPDATE** - Use new architecture |
| Create `UserCacheManagerTests.cs` | **NEW** - Unit tests with mocked adapters |
| Create `GraphUserDataLoaderTests.cs` | **NEW** - Graph loader tests |
| Create `AzureTableCacheStorageTests.cs` | **NEW** - Storage tests |

## Interface Definitions

### IUserDataLoader

```csharp
public interface IUserDataLoader
{
    /// <summary>
    /// Load all users from the data source.
    /// </summary>
    Task<UserLoadResult> LoadAllUsersAsync();

    /// <summary>
    /// Load only changed users since last sync using delta token.
    /// </summary>
    Task<UserLoadResult> LoadDeltaChangesAsync(string deltaToken);

    /// <summary>
    /// Get Copilot usage statistics for users (optional).
    /// </summary>
    Task<Dictionary<string, CopilotUserStats>> GetCopilotStatsAsync();
}

public class UserLoadResult
{
    public List<EnrichedUserInfo> Users { get; set; } = new();
    public string? DeltaToken { get; set; }
}

public class CopilotUserStats
{
    public DateTime? LastActivityDate { get; set; }
    public DateTime? CopilotChatLastActivityDate { get; set; }
    // ... other stats
}
```

### ICacheStorage

```csharp
public interface ICacheStorage
{
    /// <summary>
    /// Get all cached users (excluding deleted).
    /// </summary>
    Task<List<EnrichedUserInfo>> GetAllUsersAsync();

    /// <summary>
    /// Get a specific user by UPN.
    /// </summary>
    Task<EnrichedUserInfo?> GetUserByUpnAsync(string upn);

    /// <summary>
    /// Store or update multiple users.
    /// </summary>
    Task UpsertUsersAsync(IEnumerable<EnrichedUserInfo> users);

    /// <summary>
    /// Remove all users from cache.
    /// </summary>
    Task<int> ClearAllUsersAsync();

    /// <summary>
    /// Get cache metadata.
    /// </summary>
    Task<CacheSyncMetadata> GetSyncMetadataAsync();

    /// <summary>
    /// Update cache metadata.
    /// </summary>
    Task UpdateSyncMetadataAsync(CacheSyncMetadata metadata);
}

public class CacheSyncMetadata
{
    public string? DeltaToken { get; set; }
    public DateTime? LastFullSyncDate { get; set; }
    public DateTime? LastDeltaSyncDate { get; set; }
    public DateTime? LastCopilotStatsUpdate { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncError { get; set; }
    public int LastSyncUserCount { get; set; }
}
```

## UserCacheManager (Concrete Class)

```csharp
public class UserCacheManager : IUserCacheManager  // Simple interface for DI
{
    private readonly IUserDataLoader _dataLoader;
    private readonly ICacheStorage _storage;
    private readonly UserCacheConfig _config;
    private readonly ILogger _logger;

    public UserCacheManager(
        IUserDataLoader dataLoader,
        ICacheStorage storage,
        UserCacheConfig config,
        ILogger<UserCacheManager> logger)
    {
        _dataLoader = dataLoader;
        _storage = storage;
        _config = config;
        _logger = logger;
    }

    public async Task<List<EnrichedUserInfo>> GetAllCachedUsersAsync(bool forceRefresh = false)
    {
        var metadata = await _storage.GetSyncMetadataAsync();
        var needsSync = ShouldSync(metadata, forceRefresh);

        if (needsSync)
        {
            await SyncUsersAsync();
        }

        return await _storage.GetAllUsersAsync();
    }

    public async Task SyncUsersAsync()
    {
        var metadata = await _storage.GetSyncMetadataAsync();
        var needsFullSync = ShouldFullSync(metadata);

        try
        {
            UserLoadResult result;
            
            if (needsFullSync || string.IsNullOrEmpty(metadata.DeltaToken))
            {
                result = await _dataLoader.LoadAllUsersAsync();
                metadata.LastFullSyncDate = DateTime.UtcNow;
            }
            else
            {
                result = await _dataLoader.LoadDeltaChangesAsync(metadata.DeltaToken);
            }

            await _storage.UpsertUsersAsync(result.Users);
            
            metadata.DeltaToken = result.DeltaToken;
            metadata.LastDeltaSyncDate = DateTime.UtcNow;
            metadata.LastSyncUserCount = result.Users.Count;
            metadata.LastSyncStatus = "Success";
            
            await _storage.UpdateSyncMetadataAsync(metadata);
        }
        catch (Exception ex)
        {
            metadata.LastSyncStatus = "Failed";
            metadata.LastSyncError = ex.Message;
            await _storage.UpdateSyncMetadataAsync(metadata);
            throw;
        }
    }

    // ... other methods
}
```

## Dependency Injection (Production)

```csharp
// In DependencyInjection.cs
services.AddSingleton<IUserDataLoader>(sp =>
{
    var graphClient = sp.GetRequiredService<GraphServiceClient>();
    var logger = sp.GetRequiredService<ILogger<GraphUserDataLoader>>();
    var config = sp.GetRequiredService<UserCacheConfig>();
    return new GraphUserDataLoader(graphClient, logger, config);
});

services.AddSingleton<ICacheStorage>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AzureTableCacheStorage>>();
    var config = sp.GetRequiredService<TeamsAppConfig>();
    var cacheConfig = sp.GetRequiredService<UserCacheConfig>();
    return new AzureTableCacheStorage(
        config.ConnectionStrings.Storage, 
        logger, 
        cacheConfig);
});

services.AddSingleton<IUserCacheManager, UserCacheManager>();
```

## Testing Examples

### Unit Test with Mocked Adapters

```csharp
[Fact]
public async Task SyncUsersAsync_WithEmptyCache_PerformsFullSync()
{
    // Arrange
    var mockLoader = new Mock<IUserDataLoader>();
    var mockStorage = new Mock<ICacheStorage>();
    
    mockStorage.Setup(s => s.GetSyncMetadataAsync())
        .ReturnsAsync(new CacheSyncMetadata());
    
    mockLoader.Setup(l => l.LoadAllUsersAsync())
        .ReturnsAsync(new UserLoadResult 
        { 
            Users = CreateTestUsers(5),
            DeltaToken = "token123"
        });

    var manager = new UserCacheManager(
        mockLoader.Object,
        mockStorage.Object,
        new UserCacheConfig(),
        logger);

    // Act
    await manager.SyncUsersAsync();

    // Assert
    mockLoader.Verify(l => l.LoadAllUsersAsync(), Times.Once);
    mockStorage.Verify(s => s.UpsertUsersAsync(It.IsAny<IEnumerable<EnrichedUserInfo>>()), Times.Once);
}
```

### Integration Test with Real Adapters

```csharp
[Fact]
public async Task UserCacheManager_WithRealAdapters_WorksEndToEnd()
{
    // Arrange
    var dataLoader = new GraphUserDataLoader(graphClient, logger, config);
    var storage = new AzureTableCacheStorage(connectionString, logger, config);
    var manager = new UserCacheManager(dataLoader, storage, config, logger);

    // Act
    await manager.SyncUsersAsync();
    var users = await manager.GetAllCachedUsersAsync();

    // Assert
    Assert.True(users.Count > 0);
}
```

### Testing with Fake Loader

```csharp
[Fact]
public async Task UserCacheManager_WithFakeLoader_ReturnsTestData()
{
    // Arrange
    var fakeLoader = new FakeUserDataLoader(CreateTestUsers(10));
    var storage = new InMemoryCacheStorage();
    var manager = new UserCacheManager(fakeLoader, storage, config, logger);

    // Act
    await manager.SyncUsersAsync();
    var users = await manager.GetAllCachedUsersAsync();

    // Assert
    Assert.Equal(10, users.Count);
}
```

## Migration Path

### Step 1: Create New Interfaces and Adapters
- Create `IUserDataLoader.cs` and `ICacheStorage.cs`
- Create `GraphUserDataLoader.cs` (move delta query logic)
- Create `AzureTableCacheStorage.cs` (refactor from `UserCacheStorageService`)
- Create `InMemoryCacheStorage.cs`
- Create `FakeUserDataLoader.cs` for testing

### Step 2: Create New UserCacheManager
- Create `IUserCacheManager.cs` (simple interface)
- Create `UserCacheManager.cs` (concrete implementation)

### Step 3: Update Dependencies
- Update `DependencyInjection.cs` to register new architecture
- Update `GraphUserService.cs` to use `IUserCacheManager`
- Update `UserCacheController.cs` to use `IUserCacheManager`

### Step 4: Update Tests
- Create new unit tests with mocked adapters
- Update integration tests to use new architecture
- Test with both real and fake adapters

### Step 5: Delete Old Files
- Delete `IGraphUserCacheManager.cs`
- Delete `GraphUserCacheManager.cs`
- Delete `InMemoryUserCacheManager.cs`
- Delete `UserCacheStorageService.cs` (replaced by `AzureTableCacheStorage`)
- Delete `DeltaQueryService.cs` (logic moved to `GraphUserDataLoader`)

### Step 6: Update Documentation
- Update all documentation to reflect new architecture
- Create migration guide for users

## Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Architecture** | Abstract base class with multiple implementations | Single concrete manager with adapters |
| **Data Loading** | Mixed into manager | Clean `IUserDataLoader` interface |
| **Storage** | Mixed into manager | Clean `ICacheStorage` interface |
| **Testing** | Hard to mock Graph API | Easy to use `FakeUserDataLoader` |
| **Clarity** | Confusing inheritance | Clear adapter pattern |
| **Production** | Use specific implementation | Always use same manager, swap adapters |
| **Flexibility** | Change entire manager | Just swap an adapter |

## Timeline

| Phase | Time Estimate | Description |
|-------|---------------|-------------|
| Phase 1: Interfaces | 1 hour | Create adapter interfaces |
| Phase 2: Implementations | 3 hours | Implement all adapters |
| Phase 3: Manager | 1 hour | Create UserCacheManager |
| Phase 4: DI & Updates | 2 hours | Update all dependencies |
| Phase 5: Tests | 3 hours | Update/create all tests |
| Phase 6: Cleanup | 1 hour | Delete old files, update docs |
| **Total** | **11 hours** | Complete refactoring |

## Risk Mitigation

? **Incremental Approach**: Can be done in stages without breaking builds  
? **Parallel Development**: New architecture alongside old until cutover  
? **Comprehensive Testing**: Unit and integration tests at each stage  
? **Clear Rollback**: Keep old files until new architecture proven  
? **Documentation**: Update docs progressively  

## Decision: Proceed?

This is a significant refactoring that will result in much cleaner architecture:
- ? Proper separation of concerns
- ? Easy to test with mocks/fakes
- ? Clear adapter pattern
- ? Single concrete manager class
- ? Production-ready design

**Recommendation**: Proceed with refactoring in phases as outlined above.
