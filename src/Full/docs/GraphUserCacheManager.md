# GraphUserCacheManager Documentation

## Overview

The `GraphUserCacheManager` manages user data caching with Microsoft Graph delta query synchronization and Copilot stats integration. It provides efficient user data retrieval by caching Graph API results in Azure Table Storage.

Originally a monolithic 800-line file, it has been refactored into a modular structure with 6 focused files in the `Services/UserCache/` folder.

---

## Quick Start

```csharp
using Common.Engine.Services.UserCache;

// Initialize configuration
var config = new UserCacheConfig
{
    CacheExpiration = TimeSpan.FromHours(2),
    CopilotStatsRefreshInterval = TimeSpan.FromHours(24),
    FullSyncInterval = TimeSpan.FromDays(7),
    CopilotStatsPeriod = "D30"
};

// Create cache manager
var cacheManager = new GraphUserCacheManager(
    storageConnectionString,
    graphClient,
    logger,
    config
);

// Get all cached users (automatically syncs if needed)
var users = await cacheManager.GetAllCachedUsersAsync();

// Get specific user
var user = await cacheManager.GetCachedUserAsync("user@contoso.com");

// Manually force sync
await cacheManager.SyncUsersAsync();

// Update Copilot statistics
await cacheManager.UpdateCopilotStatsAsync();

// Clear cache
await cacheManager.ClearCacheAsync();
```

---

## Architecture

### File Structure

```
Common.Engine/Services/UserCache/
??? GraphUserCacheManager.cs        (230 lines) - Main orchestrator & public API
??? UserCacheConfig.cs              (30 lines)  - Configuration settings
??? UserCacheModels.cs              (50 lines)  - Data transfer objects
??? DeltaQueryService.cs            (110 lines) - Microsoft Graph API delta queries
??? UserCacheStorageService.cs      (170 lines) - Azure Table Storage operations
??? CopilotStatsService.cs          (180 lines) - Copilot usage statistics
```

### Component Diagram

```
???????????????????????????????????????
?   GraphUserCacheManager             ?
?   (Main Orchestrator)               ?
?   - Public API                      ?
?   - Workflow coordination           ?
???????????????????????????????????????
          ?
          ??? uses ???
          ?          ?
          ?          ?
???????????????????? ???????????????????????????
? DeltaQueryService? ? UserCacheStorageService ?
? - Graph API ops  ? ? - Table Storage ops     ?
???????????????????? ???????????????????????????
          ?
          ? uses
          ?
????????????????????????
? CopilotStatsService  ?
? - Stats retrieval    ?
? - CSV parsing        ?
????????????????????????
          ?
          ? uses
          ?
????????????????????
? UserCacheModels  ?
? - DTOs           ?
????????????????????
```

---

## Component Details

### 1. GraphUserCacheManager (Main Orchestrator)

**Purpose**: Public API and orchestration of sub-services

**Public Methods**:
```csharp
// Cache operations
Task<List<EnrichedUserInfo>> GetAllCachedUsersAsync(bool forceRefresh = false)
Task<EnrichedUserInfo?> GetCachedUserAsync(string upn)
Task ClearCacheAsync()

// Synchronization
Task SyncUsersAsync()

// Copilot statistics
Task UpdateCopilotStatsAsync()
```

**Responsibilities**:
- Exposes public API for cache operations
- Coordinates synchronization workflow (full vs delta)
- Manages Copilot stats updates
- Delegates specialized work to sub-services

**Dependencies**:
- `DeltaQueryService` - Graph API operations
- `UserCacheStorageService` - Storage operations
- `CopilotStatsService` - Copilot statistics

---

### 2. UserCacheConfig (Configuration)

**Purpose**: Configuration settings for caching behavior

```csharp
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
```

---

### 3. UserCacheModels (Data Transfer Objects)

**Purpose**: Internal models for data transfer between components

**Classes**:

```csharp
// Container for delta query results
internal class DeltaQueryResult
{
    public List<User> Users { get; set; } = new();
    public string? DeltaLink { get; set; }
}

// CSV column indices for parsing
internal class CsvColumnIndices
{
    public int UpnIndex { get; set; }
    public int LastActivityIndex { get; set; }
    public int CopilotChatIndex { get; set; }
    // ... other indices
}

// Copilot usage statistics
internal class CopilotUsageRecord
{
    public string UserPrincipalName { get; set; } = null!;
    public DateTime? LastActivityDate { get; set; }
    public DateTime? CopilotChatLastActivityDate { get; set; }
    // ... other activity dates
}
```

---

### 4. DeltaQueryService (Graph API Operations)

**Purpose**: All Microsoft Graph delta query operations

**Key Methods**:
```csharp
Task<DeltaQueryResult> FetchAllUsersAsync()
Task<DeltaQueryResult> FetchDeltaChangesAsync(string deltaLink)
```

**Responsibilities**:
- Execute delta queries against Microsoft Graph
- Handle pagination for large result sets
- Fetch full user lists or incremental changes
- Manage delta links for incremental sync

**Why Separate?**
- Graph API logic isolated from business logic
- Can be unit tested independently
- Reusable in other contexts
- Easy to mock for testing

---

### 5. UserCacheStorageService (Storage Operations)

**Purpose**: All Azure Table Storage operations

**Key Methods**:
```csharp
Task UpsertUserAsync(TableClient tableClient, User user)
Task<List<EnrichedUserInfo>> GetAllUsersAsync()
Task<EnrichedUserInfo?> GetUserByUpnAsync(string upn)
Task<int> ClearAllUsersAsync()
Task<UserSyncMetadataEntity> GetSyncMetadataAsync()
Task UpdateSyncMetadataAsync(UserSyncMetadataEntity metadata)
Task<TableClient> GetUserCacheTableClientAsync()
```

**Responsibilities**:
- CRUD operations on user cache
- Sync metadata management
- Entity-to-model mapping
- Table client management

**Why Separate?**
- Storage logic decoupled from business logic
- Easy to swap storage providers in the future
- Testable with mocked TableClient
- Clear storage abstraction

---

### 6. CopilotStatsService (Copilot Statistics)

**Purpose**: Copilot usage statistics retrieval and parsing

**Key Methods**:
```csharp
Task UpdateCachedUsersWithStatsAsync(TableClient tableClient, List<CopilotUsageRecord> stats)
Task<List<CopilotUsageRecord>> GetCopilotUsageStatsAsync()
```

**Responsibilities**:
- Fetch Copilot stats from Graph API beta endpoint
- Parse CSV format data
- Update cached users with statistics
- Handle authentication for stats API

**Why Separate?**
- Copilot functionality is optional
- Complex CSV parsing logic isolated
- Can be enabled/disabled independently
- Statistics logic separate from core caching

---

## Synchronization Workflow

### User Retrieval Flow

```
GetAllCachedUsersAsync()
  ?
Check if sync needed (cache expired?)
  ?
  Yes ? SyncUsersAsync()
    ?
    Check if full sync needed
    ?
    ?? Yes ? PerformFullSyncAsync()
    ?          ?
    ?          DeltaQueryService.FetchAllUsersAsync()
    ?          ?
    ?          UserCacheStorageService.UpsertUserAsync() (for each)
    ?
    ?? No ? PerformDeltaSyncAsync()
               ?
               DeltaQueryService.FetchDeltaChangesAsync()
               ?
               UserCacheStorageService.UpsertUserAsync() (for each change)
  ?
UserCacheStorageService.GetAllUsersAsync()
  ?
Return List<EnrichedUserInfo>
```

### Copilot Stats Update Flow

```
UpdateCopilotStatsAsync()
  ?
Check if stats refresh needed
  ?
  Yes ? CopilotStatsService.GetCopilotUsageStatsAsync()
          ?
          Fetch CSV from Graph API beta
          ?
          Parse CSV content
          ?
          CopilotStatsService.UpdateCachedUsersWithStatsAsync()
          ?
          Update each cached user with stats
  ?
Update sync metadata
```

---

## Configuration Options

### Cache Expiration
Controls how long cached data is considered fresh before triggering a delta sync.

```csharp
CacheExpiration = TimeSpan.FromHours(1)  // Default
```

**Considerations**:
- Shorter = more API calls, fresher data
- Longer = fewer API calls, potentially stale data
- Recommended: 30 minutes - 4 hours

### Copilot Stats Refresh Interval
Controls how often Copilot usage statistics are updated.

```csharp
CopilotStatsRefreshInterval = TimeSpan.FromHours(24)  // Default
```

**Considerations**:
- Copilot stats typically updated daily
- Recommended: 12-24 hours

### Full Sync Interval
Controls how often to force a complete resync instead of delta.

```csharp
FullSyncInterval = TimeSpan.FromDays(7)  // Default
```

**Considerations**:
- Full sync catches any missed deltas
- More resource intensive than delta
- Recommended: 7-14 days

### Copilot Stats Period
Report period for Copilot statistics.

```csharp
CopilotStatsPeriod = "D30"  // Default
```

**Options**:
- `D7` - Last 7 days
- `D30` - Last 30 days (recommended)
- `D90` - Last 90 days
- `D180` - Last 180 days

---

## Migration Guide

### Updating Existing Code

The public API remains unchanged. Only the namespace has changed.

**Before:**
```csharp
using Common.Engine.Services;

var manager = new GraphUserCacheManager(...);
```

**After:**
```csharp
using Common.Engine.Services.UserCache;  // Add this

var manager = new GraphUserCacheManager(...);  // Same usage
```

### Files Already Updated
- ? `GraphUserService.cs` - Updated with new using statement

### Finding Other Files

Search for references:
```powershell
# PowerShell
Get-ChildItem -Recurse -Include *.cs | Select-String "GraphUserCacheManager" -List | Select Path

# Or Git Bash
grep -r "GraphUserCacheManager" --include="*.cs" .
```

---

## Testing Strategy

### Unit Testing

Each service can now be tested independently:

```csharp
// Test DeltaQueryService
[Fact]
public async Task FetchAllUsersAsync_ReturnsUsersAndDeltaLink()
{
    // Arrange
    var mockGraphClient = new Mock<GraphServiceClient>();
    // ... setup mocks ...
    var service = new DeltaQueryService(
        mockGraphClient.Object, 
        logger, 
        userProperties
    );
    
    // Act
    var result = await service.FetchAllUsersAsync();
    
    // Assert
    Assert.NotEmpty(result.Users);
    Assert.NotNull(result.DeltaLink);
}

// Test UserCacheStorageService
[Fact]
public async Task GetUserByUpnAsync_ExistingUser_ReturnsUser()
{
    // Arrange
    var mockStorageManager = new Mock<TableStorageManager>();
    // ... setup mocks ...
    var service = new UserCacheStorageService(
        mockStorageManager.Object, 
        logger
    );
    
    // Act
    var user = await service.GetUserByUpnAsync("test@contoso.com");
    
    // Assert
    Assert.NotNull(user);
    Assert.Equal("test@contoso.com", user.UserPrincipalName);
}

// Test CopilotStatsService
[Fact]
public void ParseCopilotUsageCsv_ValidContent_ParsesCorrectly()
{
    // Arrange
    var service = new CopilotStatsService(logger, config);
    var csvContent = @"User Principal Name,Last Activity Date
test@contoso.com,2024-01-01
user@contoso.com,2024-01-02";
    
    // Act
    var records = service.ParseCopilotUsageCsv(csvContent);
    
    // Assert
    Assert.Equal(2, records.Count);
    Assert.Equal("test@contoso.com", records[0].UserPrincipalName);
}
```

### Integration Testing

```csharp
[Fact]
public async Task GetAllCachedUsersAsync_FirstCall_PerformsSync()
{
    // Arrange
    var config = new UserCacheConfig();
    var manager = new GraphUserCacheManager(
        connectionString,
        graphClient,
        logger,
        config
    );
    
    // Act
    var users = await manager.GetAllCachedUsersAsync();
    
    // Assert
    Assert.NotEmpty(users);
    // Verify sync was performed
}
```

---

## Performance Considerations

### Memory
- **Modular Loading**: Services loaded on-demand, better memory locality
- **Minimal Overhead**: Service instantiation overhead is negligible (once per lifetime)

### Compilation
- **Faster Builds**: Changes localized to specific files
- **Parallel Compilation**: Multiple files can compile simultaneously

### Runtime
- **Same Performance**: Logic unchanged, same runtime characteristics
- **Delta Queries**: Efficient incremental updates reduce API calls
- **Caching**: Reduces repeated Graph API calls

### API Throttling
- **Delta Queries**: Use fewer API calls than full queries
- **Configurable Intervals**: Control sync frequency to manage throttling
- **Metadata Tracking**: Prevents redundant syncs

---

## Future Enhancements

### 1. Interface Extraction

Create interfaces for dependency injection:

```csharp
public interface IUserCacheStorage
{
    Task UpsertUserAsync(TableClient tableClient, User user);
    Task<List<EnrichedUserInfo>> GetAllUsersAsync();
    // ... other methods
}

public interface IGraphQueryService
{
    Task<DeltaQueryResult> FetchAllUsersAsync();
    Task<DeltaQueryResult> FetchDeltaChangesAsync(string deltaLink);
}

public interface IStatsService
{
    Task<List<CopilotUsageRecord>> GetCopilotUsageStatsAsync();
    Task UpdateCachedUsersWithStatsAsync(TableClient client, List<CopilotUsageRecord> stats);
}
```

Update constructor:

```csharp
public GraphUserCacheManager(
    string storageConnectionString,
    GraphServiceClient graphClient,
    ILogger<GraphUserCacheManager> logger,
    UserCacheConfig? config = null,
    IUserCacheStorage? storageService = null,     // ? Optional DI
    IGraphQueryService? queryService = null,      // ? Optional DI
    IStatsService? statsService = null)           // ? Optional DI
    : base(storageConnectionString)
{
    _graphClient = graphClient;
    _logger = logger;
    _config = config ?? new UserCacheConfig();

    // Use provided or create default implementations
    _storageService = storageService ?? new UserCacheStorageService(this, logger);
    _queryService = queryService ?? new DeltaQueryService(graphClient, logger, UserSelectProperties);
    _statsService = statsService ?? new CopilotStatsService(logger, _config);
}
```

### 2. Alternative Storage Providers

Easily implement different storage backends:

```csharp
public class CosmosDbUserCacheStorage : IUserCacheStorage
{
    // CosmosDB implementation
}

public class SqlUserCacheStorage : IUserCacheStorage
{
    // SQL Server implementation
}

public class RedisUserCacheStorage : IUserCacheStorage
{
    // Redis implementation
}
```

### 3. Additional Stats Providers

```csharp
public class TeamsActivityStatsService : IStatsService
{
    // Teams activity statistics
}

public class SharePointUsageStatsService : IStatsService
{
    // SharePoint usage statistics
}
```

### 4. Configuration from appsettings.json

```json
{
  "UserCache": {
    "CacheExpiration": "01:00:00",
    "CopilotStatsRefreshInterval": "1.00:00:00",
    "FullSyncInterval": "7.00:00:00",
    "CopilotStatsPeriod": "D30"
  }
}
```

---

## Best Practices

### SOLID Principles Applied

- ? **Single Responsibility**: Each class has one clear purpose
- ? **Open/Closed**: Easy to extend without modifying existing code
- ? **Liskov Substitution**: Services can be substituted with alternatives
- ? **Interface Segregation**: Focused, cohesive interfaces (future)
- ? **Dependency Inversion**: Depends on abstractions via services

### Clean Code Principles

- ? Small, focused files (<250 lines each)
- ? Clear, descriptive naming
- ? Consistent patterns throughout
- ? Comprehensive XML documentation
- ? Separation of concerns

### Microsoft Conventions

- ? Async/await patterns
- ? Proper exception handling
- ? Logging at appropriate levels
- ? Nullable reference types
- ? Modern C# features (collection expressions, etc.)

---

## Troubleshooting

### Cache Not Updating

**Problem**: Users aren't seeing updated data

**Solutions**:
1. Check `CacheExpiration` setting - may be too long
2. Call `GetAllCachedUsersAsync(forceRefresh: true)` to force sync
3. Check Graph API permissions
4. Review logs for sync errors

### Copilot Stats Not Available

**Problem**: Copilot statistics aren't being populated

**Solutions**:
1. Verify Graph API beta access
2. Check `CopilotStatsRefreshInterval` setting
3. Ensure proper authentication for beta API
4. Implement `GetAccessTokenAsync()` method (currently throws `NotImplementedException`)

### Delta Link Expired

**Problem**: Delta sync fails with expired link error

**Solutions**:
- Delta links expire after 7 days
- Automatic fallback to full sync is built-in
- Consider reducing `FullSyncInterval` if this happens frequently

### High API Usage

**Problem**: Too many Graph API calls

**Solutions**:
1. Increase `CacheExpiration` duration
2. Increase `CopilotStatsRefreshInterval`
3. Use delta queries (already default)
4. Monitor sync frequency

---

## Metrics & Monitoring

### Key Metrics to Track

```csharp
// Sync metrics
- Last full sync date
- Last delta sync date
- User count synchronized
- Sync status (success/failed)
- Sync error messages

// Cache metrics
- Cache hit rate
- Cache size (user count)
- Cache age

// Copilot metrics
- Last stats update
- Stats coverage (% of users with data)
```

### Logging Levels

```csharp
// Information
_logger.LogInformation("Full sync completed: {userCount} users synchronized");
_logger.LogInformation("Copilot stats are still fresh, skipping update");

// Warning
_logger.LogWarning("No delta link available, performing full sync instead");

// Error
_logger.LogError(ex, "Error during user sync");
_logger.LogError(ex, "Error updating Copilot stats");

// Debug
_logger.LogDebug($"Retrieved user {upn} from cache");
_logger.LogDebug($"User {upn} not found in cache, skipping stats update");
```

---

## Summary

### What Was Achieved

**Before Refactoring**:
- ? Single 800-line file
- ? Multiple responsibilities mixed
- ? Hard to navigate and maintain
- ? Difficult to test
- ? Low code reuse

**After Refactoring**:
- ? 6 focused files (avg 128 lines)
- ? Single responsibility per file
- ? Easy to find specific functionality
- ? Independently testable services
- ? Clean separation of concerns
- ? Professional code organization

### Benefits Delivered

1. **Maintainability**: Changes localized to specific files
2. **Testability**: Each component independently testable
3. **Readability**: Small, focused files easy to understand
4. **Extensibility**: Easy to add new features or swap implementations
5. **Professional**: Follows industry best practices and SOLID principles

### No Breaking Changes

Existing code continues to work with just a namespace update:
```csharp
using Common.Engine.Services.UserCache;  // Add this line
```

---

## Additional Resources

### Related Files
- `Common.Engine/Storage/UserCacheEntities.cs` - Table entity definitions
- `Common.Engine/Services/GraphUserService.cs` - User service using cache manager

### Microsoft Documentation
- [Microsoft Graph Delta Query](https://docs.microsoft.com/en-us/graph/delta-query-overview)
- [Azure Table Storage](https://docs.microsoft.com/en-us/azure/storage/tables/)
- [Microsoft Graph Reports API](https://docs.microsoft.com/en-us/graph/api/resources/report)

### Support
For questions or issues, check:
1. This documentation
2. XML comments in code
3. Unit test examples
4. Team collaboration channels
