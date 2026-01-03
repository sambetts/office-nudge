# Summary: Using PendingCardConversationResumeHandler in Dependency Injection

## Overview
Updated the application to use `PendingCardConversationResumeHandler` instead of `DefaultConversationResumeHandler` to automatically send pending adaptive cards to users when they interact with the bot.

## Files Modified

### 1. `Web\Web.Server\Program.cs`
**Changes:**
- Replaced `DefaultConversationResumeHandler` with `PendingCardConversationResumeHandler`
- Changed registration from `IConversationResumeHandler<string>` to `IConversationResumeHandler<PendingCardInfo>`
- Changed service lifetime from `Singleton` to `Scoped` because `PendingCardLookupService` is registered as scoped

**Before:**
```csharp
builder.Services.AddSingleton<IConversationResumeHandler<string>, DefaultConversationResumeHandler>();
```

**After:**
```csharp
builder.Services.AddScoped<IConversationResumeHandler<PendingCardInfo>, PendingCardConversationResumeHandler>();
```

### 2. `Common.Engine\Notifications\BotConvoResumeManager.cs`
**Changes:**
- Replaced direct injection of `IConversationResumeHandler<PendingCardInfo>` with `IServiceProvider`
- Added scoped service resolution using `serviceProvider.CreateScope()`
- This allows the singleton `BotConvoResumeManager` to safely consume the scoped `IConversationResumeHandler<PendingCardInfo>`
- Added using statement for `Microsoft.Extensions.DependencyInjection` namespace

**Before:**
```csharp
public class BotConvoResumeManager(...,
    IConversationResumeHandler<string> conversationResumeHandler,
    ...)
{
    // Direct use of conversationResumeHandler
    var (data, card) = await conversationResumeHandler.LoadDataAndResumeConversation(upn);
}
```

**After:**
```csharp
public class BotConvoResumeManager(...,
    IServiceProvider serviceProvider,
    ...)
{
    // Create scope and resolve scoped handler
    using var scope = serviceProvider.CreateScope();
    var conversationResumeHandler = scope.ServiceProvider.GetRequiredService<IConversationResumeHandler<PendingCardInfo>>();
    var (data, card) = await conversationResumeHandler.LoadDataAndResumeConversation(upn);
}
```

**Why this change?**
- `BotConvoResumeManager` is registered as a Singleton (long-lived)
- `PendingCardLookupService` is registered as Scoped (per-request lifetime)
- Direct injection would cause: `Cannot consume scoped service from singleton`
- Solution: Use `IServiceProvider` to create a scope and resolve scoped services on-demand

### 3. `Common.Engine\Services\MessageSenderService.cs`
**Changes:**
- Replaced direct injection of `MessageTemplateService` with `IServiceProvider`
- Added scoped service resolution using `serviceProvider.CreateScope()` in multiple places
- This allows the singleton `MessageSenderService` to safely consume the scoped `MessageTemplateService`
- Added using statement for `Microsoft.Extensions.DependencyInjection` namespace

**Before:**
```csharp
public class MessageSenderService(...,
    MessageTemplateService templateService,
    ...)
{
    // Direct use of templateService
    await templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Success");
}
```

**After:**
```csharp
public class MessageSenderService(...,
    IServiceProvider serviceProvider,
    ...)
{
    // Create scope and resolve scoped service
    using var scope = serviceProvider.CreateScope();
    var templateService = scope.ServiceProvider.GetRequiredService<MessageTemplateService>();
    await templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Success");
}
```

**Why this change?**
- `MessageSenderService` is registered as a Singleton
- `MessageTemplateService` is registered as Scoped
- Direct injection would cause: `Cannot consume scoped service 'MessageTemplateService' from singleton 'MessageSenderService'`
- Solution: Use `IServiceProvider` to create a scope and resolve scoped services on-demand

### 4. `Web\Web.Server\Bots\TeamsBot.cs`
**Changes:**
- Updated constructor to use `IConversationResumeHandler<PendingCardInfo>` instead of `IConversationResumeHandler<string>`
- Added using statement for `Common.Engine.Services` namespace

**Before:**
```csharp
public class TeamsBot<T>(...,
    IConversationResumeHandler<string> conversationResumeHandler)
```

**After:**
```csharp
public class TeamsBot<T>(...,
    IConversationResumeHandler<PendingCardInfo> conversationResumeHandler)
```

**Note:** `TeamsBot` is registered as `Transient` in `Program.cs`, so it can safely inject scoped services directly.

## Service Lifetime Architecture

```
???????????????????????????????????????????????
? Singleton Services (App Lifetime)          ?
???????????????????????????????????????????????
? - BotConvoResumeManager (uses IServiceProvider) ?
? - MessageSenderService (uses IServiceProvider)  ?
? - BatchMessageProcessorService              ?
? - MessageTemplateStorageManager             ?
? - BatchQueueService                         ?
???????????????????????????????????????????????
                    ? creates scope
???????????????????????????????????????????????
? Scoped Services (Per Request)               ?
???????????????????????????????????????????????
? - IConversationResumeHandler<PendingCardInfo> ?
? - PendingCardLookupService                  ?
? - MessageTemplateService                    ?
???????????????????????????????????????????????
                    ? injected directly
???????????????????????????????????????????????
? Transient Services (Per Use)                ?
???????????????????????????????????????????????
? - TeamsBot<T>                               ?
???????????????????????????????????????????????
```

## Behavior Changes

### Before
- Bot would send a generic "Welcome Back!" message to all users when they resumed a conversation
- No awareness of pending adaptive cards in the message queue

### After
- Bot automatically checks for pending cards when users interact
- If pending cards exist:
  - Sends the latest pending adaptive card from Azure Table Storage
  - Card is retrieved based on the user's UPN
  - Uses the actual template that was scheduled for the user
- If no pending cards exist:
  - Sends a default welcome message indicating no pending messages

## How It Works

1. **User interacts with bot** ? `TeamsBot.OnMembersAddedAsync` is triggered
2. **Bot checks conversation cache** ? Determines if user has spoken before
3. **Resume conversation handler is called** ? `PendingCardConversationResumeHandler.LoadDataAndResumeConversation`
4. **Lookup pending cards** ? `PendingCardLookupService.GetLatestPendingCardByUpn` queries Azure Table Storage
5. **Send appropriate card**:
   - If pending card found ? Send the adaptive card from template
   - If no pending card ? Send default welcome message

## Dependencies

The `PendingCardConversationResumeHandler` requires:
- `PendingCardLookupService` (automatically registered via `AddMessageTemplateServices`)
- `ILogger<PendingCardConversationResumeHandler>` (provided by DI container)

### Service Lifetimes:
- `IBotConvoResumeManager` / `BotConvoResumeManager` ? **Singleton** (uses IServiceProvider)
- `MessageSenderService` ? **Singleton** (uses IServiceProvider)
- `IConversationResumeHandler<PendingCardInfo>` / `PendingCardConversationResumeHandler` ? **Scoped**
- `PendingCardLookupService` ? **Scoped**
- `MessageTemplateService` ? **Scoped**
- `TeamsBot<T>` ? **Transient**

## Error Resolution

### Problem 1: BotConvoResumeManager
Initial implementation caused runtime error:
```
Cannot consume scoped service 'Common.Engine.Services.PendingCardLookupService' 
from singleton 'Common.Engine.Notifications.IBotConvoResumeManager'
```

**Solution:**
Updated `BotConvoResumeManager` to:
1. Accept `IServiceProvider` instead of direct handler injection
2. Create a scope when handler is needed: `using var scope = serviceProvider.CreateScope()`
3. Resolve handler from scope: `scope.ServiceProvider.GetRequiredService<IConversationResumeHandler<PendingCardInfo>>()`

### Problem 2: MessageSenderService
Second runtime error:
```
Cannot consume scoped service 'Common.Engine.Services.MessageTemplateService' 
from singleton 'Common.Engine.Services.MessageSenderService'
```

**Solution:**
Updated `MessageSenderService` to:
1. Accept `IServiceProvider` instead of direct `MessageTemplateService` injection
2. Create scopes when service is needed (in both success and error paths)
3. Resolve service from scope: `scope.ServiceProvider.GetRequiredService<MessageTemplateService>()`

This is a standard pattern for singleton services that need to consume scoped services.

## Testing

To test the changes:

1. **Create a message batch** with adaptive cards for specific users
2. **Ensure status is "Pending"** in Azure Table Storage (messagelogs table)
3. **Open Teams** and interact with the bot as one of the target users
4. **Expected result**: The bot should send the pending adaptive card automatically

## Rollback

To rollback to the previous behavior, change `Program.cs` back to:

```csharp
builder.Services.AddSingleton<IConversationResumeHandler<string>, DefaultConversationResumeHandler>();
```

And revert:
- `BotConvoResumeManager.cs` to use direct injection of `IConversationResumeHandler<string>`
- `MessageSenderService.cs` to use direct injection of `MessageTemplateService`

## Build Status

? Build successful with all changes applied.

## Related Documentation

- [PendingCardLookupService README](../Common.Engine/Services/PendingCardLookupService.README.md)
- [PendingCardConversationResumeHandler](../Web/Web.Server/Bots/PendingCardConversationResumeHandler.cs)
- [Service Lifetimes in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Consuming Scoped Services from Singletons](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#scoped-service-as-singleton)
