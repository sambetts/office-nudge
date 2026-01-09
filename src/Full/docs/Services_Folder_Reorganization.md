# Services Folder Reorganization Summary

## Overview

Reorganized the `Common.Engine\Services` folder to separate actual services from data models and configuration classes, improving code organization and maintainability.

---

## What Was Moved

### 1. Models (Services ? Models)

**Created**: `Common.Engine\Models\` folder

**Files Moved**:

| Old Location | New Location | Type |
|---|---|---|
| `Services\EnrichedUserInfo.cs` | `Models\EnrichedUserInfo.cs` | Data Model |
| `Services\UserCache\UserCacheModels.cs` | `Models\UserCacheModels.cs` | Data Models |
| `CachedUserAndConversationData.cs` | `Models\CachedUserAndConversationData.cs` | Entity |
| (Extracted from BotUserUtils.cs) | `Models\BotUser.cs` | Data Model |

**Classes in Models**:
- `EnrichedUserInfo` - User information with metadata
- `DeltaQueryResult` - Delta query result container
- `CsvColumnIndices` - CSV parsing helper
- `CopilotUsageRecord` - Copilot usage statistics
- `CachedUserAndConversationData` - Conversation cache entity
- `BotUser` - Bot user identification model

---

### 2. Configuration (Services ? Config)

**Files Moved**:

| Old Location | New Location | Type |
|---|---|---|
| `Services\UserCache\UserCacheConfig.cs` | `Config\UserCacheConfig.cs` | Configuration |

**Why**: Configuration classes belong in the Config folder, not Services

---

## Namespace Changes

### Before
```csharp
using Common.Engine.Services;                    // EnrichedUserInfo
using Common.Engine.Services.UserCache;          // UserCacheConfig, UserCacheModels
using Common.Engine;                             // CachedUserAndConversationData, BotUser
```

### After
```csharp
using Common.Engine.Models;                      // EnrichedUserInfo, BotUser, CachedUserAndConversationData
                                                  // DeltaQueryResult, CsvColumnIndices, CopilotUsageRecord
using Common.Engine.Config;                      // UserCacheConfig
```

---

## Files Requiring Updates

### ? Already Updated
1. `Common.Engine\Services\GraphUserService.cs`
2. `Common.Engine\Services\UserCache\*.cs` (all files)

### ? Need Updates
1. `Common.Engine\Services\AIFoundryService.cs` - Add `using Common.Engine.Models;`
2. `Common.Engine\Services\SmartGroupService.cs` - Add `using Common.Engine.Models;`
3. `Common.Engine\Notifications\BotConvoResumeManager.cs` - Add `using Common.Engine.Models;`
4. `Common.Engine\BotConversationCache.cs` - Add `using Common.Engine.Models;`
5. `Common.Engine\BotUserUtils.cs` - Add `using Common.Engine.Models;` and remove duplicate BotUser class
6. `UnitTests\Services\UserCache\InMemoryUserCacheManagerTests.cs` - Add `using Common.Engine.Config;` and `using Common.Engine.Models;`

---

## Quick Fix Script

```powershell
# Add using statements to files
$filesToFix = @(
    @{Path="Common.Engine\Services\AIFoundryService.cs"; Using="using Common.Engine.Models;"},
    @{Path="Common.Engine\Services\SmartGroupService.cs"; Using="using Common.Engine.Models;"},
    @{Path="Common.Engine\Notifications\BotConvoResumeManager.cs"; Using="using Common.Engine.Models;"},
    @{Path="Common.Engine\BotConversationCache.cs"; Using="using Common.Engine.Models;"},
    @{Path="UnitTests\Services\UserCache\InMemoryUserCacheManagerTests.cs"; Using="using Common.Engine.Config;`nusing Common.Engine.Models;"}
)

foreach ($file in $filesToFix) {
    if (Test-Path $file.Path) {
        $content = Get-Content $file.Path -Raw
        if ($content -notmatch [regex]::Escape($file.Using)) {
            # Add after existing using statements
            $content = $content -replace '(using [^;]+;\s*\n)+', "`$0$($file.Using)`n"
            Set-Content $file.Path -Value $content
        }
    }
}

# Fix BotUserUtils.cs - remove duplicate BotUser class
$botUserUtils = Get-Content "Common.Engine\BotUserUtils.cs" -Raw
if ($botUserUtils -match "public class BotUser") {
    $botUserUtils = $botUserUtils -replace 'public class BotUser\s*\{[^\}]+\}', ''
    Set-Content "Common.Engine\BotUserUtils.cs" -Value $botUserUtils
}
```

---

## Folder Structure

### Before
```
Common.Engine/
??? Services/
?   ??? EnrichedUserInfo.cs                      ? Not a service
?   ??? GraphUserService.cs                      ? Service
?   ??? SmartGroupService.cs                     ? Service
?   ??? UserCache/
?       ??? UserCacheConfig.cs                   ? Not a service (config)
?       ??? UserCacheModels.cs                   ? Not a service (models)
?       ??? GraphUserCacheManager.cs             ? Service
?       ??? ...
??? CachedUserAndConversationData.cs             ? Not a service (model)
??? BotUserUtils.cs                              ? Utilities (contains BotUser model ?)
```

### After
```
Common.Engine/
??? Models/                                       ? NEW
?   ??? EnrichedUserInfo.cs                     ? Model
?   ??? UserCacheModels.cs                      ? Models
?   ??? BotUser.cs                              ? Model
?   ??? CachedUserAndConversationData.cs        ? Entity
??? Config/
?   ??? UserCacheConfig.cs                      ? Configuration
?   ??? ... (existing configs)
??? Services/
?   ??? GraphUserService.cs                     ? Service only
?   ??? SmartGroupService.cs                    ? Service only
?   ??? UserCache/
?       ??? GraphUserCacheManager.cs            ? Service only
?       ??? InMemoryUserCacheManager.cs         ? Service only
?       ??? DeltaQueryService.cs                ? Service only
?       ??? UserCacheStorageService.cs          ? Service only
?       ??? CopilotStatsService.cs              ? Service only
??? BotUserUtils.cs                             ? Utilities only
```

---

## Benefits

### 1. Clear Separation of Concerns
- **Models** folder contains only data structures
- **Config** folder contains only configuration
- **Services** folder contains only service implementations

### 2. Better Discoverability
- Easy to find all models in one place
- Easy to find all configs in one place
- Services folder is cleaner and more focused

### 3. Improved Maintainability
- Related classes grouped together
- Clear purpose for each folder
- Easier to navigate codebase

### 4. Standard Project Structure
- Follows common .NET project conventions
- Models/Entities separate from business logic
- Configuration separate from implementation

---

## Service Definition

**A service is a class that**:
- Provides business logic or functionality
- Orchestrates operations
- Typically has dependencies injected
- Usually registered in DI container
- Often implements an interface

**Not a service**:
- Data models / DTOs
- Configuration classes
- Entities
- Utility/helper classes with no state

---

## Migration Checklist

- [x] Create `Models` folder
- [x] Move `EnrichedUserInfo` to Models
- [x] Move `UserCacheModels` to Models
- [x] Extract `BotUser` to Models
- [x] Move `CachedUserAndConversationData` to Models
- [x] Move `UserCacheConfig` to Config
- [x] Update `GraphUserService.cs`
- [x] Update all UserCache services
- [ ] Update `AIFoundryService.cs`
- [ ] Update `SmartGroupService.cs`
- [ ] Update `BotConvoResumeManager.cs`
- [ ] Update `BotConversationCache.cs`
- [ ] Update `BotUserUtils.cs`
- [ ] Update unit tests
- [ ] Remove old files
- [ ] Build and verify

---

## Status

**Completed**:
- ? Folder structure created
- ? Files moved to new locations
- ? Core services updated

**Remaining**:
- ? Update remaining files with new using statements
- ? Remove duplicate BotUser class from BotUserUtils
- ? Verify build

---

## Example: How to Use Models

```csharp
using Common.Engine.Models;
using Common.Engine.Config;
using Common.Engine.Services.UserCache;

namespace MyNamespace;

public class MyService
{
    private readonly GraphUserCacheManagerBase _cacheManager;
    private readonly UserCacheConfig _config;
    
    public async Task<List<EnrichedUserInfo>> GetUsersAsync()
    {
        return await _cacheManager.GetAllCachedUsersAsync();
    }
    
    public BotUser GetBotUser(string userId)
    {
        return new BotUser { UserId = userId, IsAzureAdUserId = true };
    }
}
```

---

## Summary

**What**: Reorganized Services folder to separate models and configuration from actual services  
**Why**: Better code organization, clearer structure, follows .NET conventions  
**Impact**: Namespace changes required in files that use the moved classes  
**Benefit**: Cleaner, more maintainable codebase with clear separation of concerns  

**Next Step**: Apply the Quick Fix Script above to update remaining files and complete the migration.
