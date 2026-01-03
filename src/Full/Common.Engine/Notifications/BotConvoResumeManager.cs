using Common.Engine.Config;
using Common.Engine.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Common.Engine.Notifications;

public class BotConvoResumeManager(ILogger<BotConvoResumeManager> loggerBotConvoResumeManager,
    ILogger<BotAppInstallHelper> loggerBotAppInstallHelper,
    BotConversationCache botConversationCache,
    IServiceProvider serviceProvider,
    GraphServiceClient graphServiceClient, TeamsAppConfig config, IBotFrameworkHttpAdapter adapter) : IBotConvoResumeManager
{
    private const string TeamsBotFrameworkChannelId = "msteams";

    private readonly ILogger _loggerBotConvoResumeManager = loggerBotConvoResumeManager;

    /// <summary>
    /// Resumes a conversation with the specified user in Microsoft Teams, installing the bot app for the user if
    /// necessary.
    /// </summary>
    /// <remarks>If a conversation with the specified user already exists, this method resumes it by sending a
    /// message. If no conversation exists, the bot app is installed for the user and a new conversation is initiated.
    /// The method logs warnings if the user cannot be found or if the bot app cannot be installed. The user must be
    /// licensed for Microsoft Teams for the operation to succeed.</remarks>
    /// <param name="upn">The user principal name (UPN) of the user with whom to resume the conversation. Cannot be null or empty.</param>
    /// <returns>A result object indicating success status and an operation message</returns>
    public async Task<ConversationResumeResult> ResumeConversation(string upn)
    {
        // Get AAD user ID from Graph by looking up user by email
        User? graphUser = null;
        try
        {
            graphUser = await graphServiceClient.Users[upn].GetAsync(op => op.QueryParameters.Select = ["Id"]);
        }
        catch (ODataError ex)
        {
            var message = $"Couldn't get user by UPN '{upn}' - {ex.Message}";
            _loggerBotConvoResumeManager.LogWarning(message);
            return new ConversationResumeResult { Success = false, Message = message };
        }
        if (graphUser?.Id != null)
        {
            // Do we have a conversation with this user yet?
            if (botConversationCache.ContainsUserId(graphUser.Id))
            {
                var cachedUser = botConversationCache.GetCachedUser(graphUser.Id)!;
                var convoId = cachedUser.ConversationId;

                var previousConversationReference = new ConversationReference()
                {
                    ChannelId = TeamsBotFrameworkChannelId,
                    Bot = new ChannelAccount() { Id = $"28:{config.AppCatalogTeamAppId}" },
                    ServiceUrl = cachedUser.ServiceUrl,
                    Conversation = new ConversationAccount() { Id = cachedUser.ConversationId },
                };

                try
                {
                    // Create a scope to resolve scoped services (like PendingCardLookupService)
                    using var scope = serviceProvider.CreateScope();
                    var conversationResumeHandler = scope.ServiceProvider.GetRequiredService<IConversationResumeHandler<PendingCardInfo>>();
                    
                    // Continue conversation with the registered "resume conversation" service
                    var (data, card) = await conversationResumeHandler.LoadDataAndResumeConversation(upn);

                    var resumeActivity = MessageFactory.Attachment(card);

                    await ((CloudAdapter)adapter)
                        .ContinueConversationAsync(config.GraphConfig.ClientId, previousConversationReference,
                        async (turnContext, cancellationToken) =>
                            await turnContext.SendActivityAsync(resumeActivity, cancellationToken), CancellationToken.None);
                    
                    return new ConversationResumeResult { Success = true, Message = $"Message sent successfully to {upn}" };
                }
                catch (Exception ex)
                {
                    var message = $"Error sending message to {upn}: {ex.Message}";
                    _loggerBotConvoResumeManager.LogError(ex, message);
                    return new ConversationResumeResult { Success = false, Message = message };
                }
            }
            else
            {
                // No conversation with this user yet, so install the bot app for them
                if (string.IsNullOrEmpty(config.AppCatalogTeamAppId))
                {
                    var message = $"Can't install Teams app for bot - no {nameof(config.AppCatalogTeamAppId)} found in configuration";
                    _loggerBotConvoResumeManager.LogError(message);
                    return new ConversationResumeResult { Success = false, Message = message };
                }
                else
                {
                    var installManager = new BotAppInstallHelper(loggerBotAppInstallHelper, graphServiceClient);
                    try
                    {
                        // Install app and if already installed, trigger a new conversation update.
                        // This will then be picked up by the bot and the conversation ID then cached for this user.
                        await installManager.InstallBotForUser(graphUser.Id, config.AppCatalogTeamAppId,
                            async () => await TriggerUserConversationUpdate(graphUser.Id, config.AppCatalogTeamAppId, installManager));
                        
                        return new ConversationResumeResult { Success = true, Message = $"Bot app installed for {upn}. Conversation will be initiated when user opens the app." };
                    }
                    catch (ODataError ex)
                    {
                        var message = $"Couldn't install Teams app for user '{graphUser.Id}' - {ex.Message} - is user licensed for Teams?";
                        _loggerBotConvoResumeManager.LogWarning(message);
                        return new ConversationResumeResult { Success = false, Message = message };
                    }
                }
            }
        }
        
        return new ConversationResumeResult { Success = false, Message = $"User {upn} not found or has no ID" };
    }

    async Task TriggerUserConversationUpdate(string userid, string appId, BotAppInstallHelper installManager)
    {
        _loggerBotConvoResumeManager.LogInformation($"Triggering new conversation with bot {appId} for user {userid}");

        // Docs here: https://docs.microsoft.com/en-us/microsoftteams/platform/graph-api/proactive-bots-and-messages/graph-proactive-bots-and-messages#-retrieve-the-conversation-chatid
        var installedApp = await installManager.GetUserInstalledApp(userid, appId);
        try
        {
            var chat = await graphServiceClient.Users[userid].Teamwork.InstalledApps[installedApp.Id].Chat.GetAsync();
        }
        catch (ODataError ex)
        {
            _loggerBotConvoResumeManager.LogWarning($"Couldn't get chat for user '{userid}' - {ex.Message}");
        }
    }
}

public interface IBotConvoResumeManager
{
    public abstract Task<ConversationResumeResult> ResumeConversation(string upn);
}

/// <summary>
/// Result of a conversation resume operation
/// </summary>
public class ConversationResumeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
