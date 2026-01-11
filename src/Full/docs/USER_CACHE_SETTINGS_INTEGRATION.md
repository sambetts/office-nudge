# User Cache Management - Settings Page Integration

## Overview
Added user cache management functionality to the Settings page, allowing administrators to clear and sync the Microsoft Graph user cache directly from the UI.

## Changes Made

### Backend Changes

#### 1. New Controller: `UserCacheController.cs`
**Location:** `Web/Web.Server/Controllers/UserCacheController.cs`

**Endpoints:**
- `POST /api/UserCache/Clear` - Clears the entire user cache and forces a full resync
- `POST /api/UserCache/Sync` - Forces immediate synchronization of users from Microsoft Graph
- `GET /api/UserCache/Status` - Gets current cache status (user count, cache type)

**Features:**
- ? Requires authentication (uses `[Authorize]` attribute)
- ? Logs all cache operations with user identity
- ? Returns user-friendly messages
- ? Handles errors gracefully

**DTOs:**
```csharp
public class UserCacheStatusDto
{
    public int UserCount { get; set; }
    public bool IsInMemory { get; set; }
    public string CacheType { get; set; }
}
```

### Frontend Changes

#### 2. Updated API Calls: `ApiCalls.ts`
**Location:** `Web/web.client/src/api/ApiCalls.ts`

**New Functions:**
```typescript
getCacheStatus(loader: BaseAxiosApiLoader): Promise<UserCacheStatusDto>
clearUserCache(loader: BaseAxiosApiLoader): Promise<{ message: string }>
syncUserCache(loader: BaseAxiosApiLoader): Promise<{ message: string }>
```

#### 3. Updated Models: `Models.ts`
**Location:** `Web/web.client/src/apimodels/Models.ts`

**New Interface:**
```typescript
export interface UserCacheStatusDto {
  userCount: number;
  isInMemory: boolean;
  cacheType: string;
}
```

#### 4. Enhanced Settings Page: `SettingsPage.tsx`
**Location:** `Web/web.client/src/pages/Settings/SettingsPage.tsx`

**New Features:**
- ?? **Cache Status Display** - Shows current cache type and user count
- ?? **Sync Cache Button** - Triggers immediate cache synchronization
- ??? **Clear Cache Button** - Clears cache with confirmation dialog
- ?? **In-Memory Warning** - Displays info banner when using in-memory cache
- ? **Loading States** - Proper loading indicators during operations
- ? **Success/Error Messages** - User feedback for all operations

**UI Layout:**
```
?? Settings Page ???????????????????????
?                                      ?
?  [User Cache Management Card]        ?
?  ?????????????????????????????????? ?
?  ? Cache Status: [Badge]          ? ?
?  ? Cached Users: 1,234            ? ?
?  ?                                ? ?
?  ? [Sync Cache] [Clear Cache]     ? ?
?  ?????????????????????????????????? ?
?                                      ?
?  [Follow-up Chat System Prompt Card] ?
?  ?????????????????????????????????? ?
????????????????????????????????????????
```

## User Experience

### Sync Cache Flow
1. User clicks "Sync Cache" button
2. Button shows "Syncing..." with loading state
3. Backend performs delta/full sync from Microsoft Graph
4. Cache status is refreshed automatically
5. Success message displayed: "User cache synchronized successfully."

### Clear Cache Flow
1. User clicks "Clear Cache" button
2. Confirmation dialog: "Are you sure you want to clear the user cache?"
3. User confirms
4. Button shows "Clearing..." with loading state
5. Backend clears all cached users and resets delta link
6. Cache status is refreshed (shows 0 users)
7. Success message: "User cache cleared successfully. A full sync will occur on next access."

## Cache Types Displayed

The page shows different cache implementations:
- **InMemoryUserCacheManager** - In-memory cache (development)
  - Shows info banner: "Using in-memory cache. Cache will be lost on application restart."
- **GraphUserCacheManager** - Azure Table Storage (production)
  - No warning banner

## Security

- ? All endpoints require authentication
- ? User identity logged for all cache operations
- ? Confirmation dialogs for destructive actions
- ? Proper error handling and user feedback

## Integration with Existing Features

### Smart Groups
Smart groups use `GraphUserService` which depends on the cache:
- After clearing cache, smart group resolution will trigger a full sync
- Syncing cache ensures smart groups have up-to-date user data

### Message Sending
User lookups for message delivery benefit from synced cache:
- Faster user lookups
- Reduced Graph API throttling

### Copilot Connected Mode
Cache synchronization works alongside Copilot features:
- Copilot stats remain separate from user sync operations
- Both use the same cache infrastructure

## Testing

### Manual Testing Steps

1. **View Cache Status**
   - Navigate to Settings page
   - Verify cache status is displayed correctly
   - Check user count matches expected tenant size

2. **Sync Cache**
   - Click "Sync Cache" button
   - Verify loading state appears
   - Wait for success message
   - Check cache status updates

3. **Clear Cache**
   - Click "Clear Cache" button
   - Confirm dialog
   - Verify success message
   - Check user count shows 0 (or refreshes to actual count after auto-sync)

4. **Error Handling**
   - Disconnect from Azure Storage
   - Try cache operations
   - Verify error messages display properly

### API Testing with Swagger

```
POST https://your-app.azurewebsites.net/api/UserCache/Clear
POST https://your-app.azurewebsites.net/api/UserCache/Sync
GET https://your-app.azurewebsites.net/api/UserCache/Status
```

## Performance Considerations

### Sync Operation
- **Delta Sync** (~1-10 seconds for incremental changes)
- **Full Sync** (~30-60 seconds for 1,000 users)
- Operation runs asynchronously
- UI remains responsive during sync

### Clear Operation
- **Fast** (~1-2 seconds for in-memory)
- **Moderate** (~5-10 seconds for Azure Table Storage with 1,000+ users)
- Next cache access triggers full sync

## Logging

All operations are logged with:
- User identity (UPN)
- Operation type (Clear/Sync/Status)
- Timestamp
- Success/failure status
- Error details (if applicable)

**Example Logs:**
```
[Information] User cache sync requested by admin@contoso.com
[Information] Performing delta sync...
[Information] Delta sync completed: 42 changes processed
[Information] User cache synchronized successfully

[Information] User cache clear requested by admin@contoso.com
[Information] Cleared 1,234 users from cache
```

## Future Enhancements

Potential additions:
1. **Schedule Automatic Syncs** - Cron-like scheduling UI
2. **Cache Metrics** - Hit/miss ratios, performance graphs
3. **Delta Link Display** - Show last delta token for debugging
4. **Sync History** - Table of past sync operations
5. **User Search** - Search cached users by name/UPN
6. **Manual User Refresh** - Refresh specific users instead of all

## Related Documentation

- [User Cache Implementation](../../docs/USER_CACHING_IMPLEMENTATION.md)
- [GraphUserCacheManager Abstraction](../../docs/GraphUserCacheManager_Abstraction_Summary.md)
- [Settings Page Documentation](./SettingsPage.README.md)

## Build Status

? **Backend Build**: Passing  
? **Frontend Build**: Passing  
? **TypeScript Compilation**: Passing  
? **No Breaking Changes**: All existing features work  

## Summary

Successfully added user cache management to the Settings page with:
- ? New backend controller with 3 endpoints
- ? Frontend API integration
- ? Enhanced Settings UI with cache management card
- ? Proper error handling and user feedback
- ? Confirmation dialogs for destructive actions
- ? Real-time cache status display
- ? Support for both in-memory and Azure Table Storage caches
- ? Full authentication and authorization
- ? Comprehensive logging
