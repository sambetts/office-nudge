using Common.Engine;
using Common.Engine.Config;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Web.Server.Bots.Dialogues.Abstract;

namespace Web.Server.Bots.Dialogues;


/// <summary>
/// Entrypoint to all new conversations
/// </summary>
public class MainDialogue : CommonBotDialogue
{
    private readonly UserState _userState;

    const string CACHE_NAME_CONVO_STATE = "CACHE_NAME_CONVO_STATE";

    /// <summary>
    /// Setup dialogue flow
    /// </summary>
    public MainDialogue(BotConfig configuration, BotConversationCache botConversationCache, ILogger<MainDialogue> tracer,
        BotActionsHelper botActionsHelper,
        UserState userState)
        : base(nameof(MainDialogue), botConversationCache, configuration)
    {
        _userState = userState;
        AddDialog(new TextPrompt(nameof(TextPrompt)));

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog),
        [
            NewChat
        ]));
        AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        InitialDialogId = nameof(WaterfallDialog);
    }

    /// <summary>
    /// Main entry-point for bot new chat. User is either responding to the intro card or has said something to the bot.
    /// </summary>
    private async Task<DialogTurnResult> NewChat(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get/set state
        var convoState = await GetConvoStateAsync(stepContext.Context);

        await SendMsg(stepContext.Context!,
                        "Hi! Make your bot do something!"
                     );
        return await stepContext.EndDialogAsync();
    }

    async Task<MainDialogueConvoState> GetConvoStateAsync(ITurnContext context)
    {
        var convoStateProp = _userState.CreateProperty<MainDialogueConvoState>(CACHE_NAME_CONVO_STATE);
        var convoState = await convoStateProp.GetAsync(context);
        if (convoState == null)
        {
            convoState = new MainDialogueConvoState();
            await convoStateProp.SetAsync(context, convoState);
        }
        return convoState;
    }

}

internal class MainDialogueConvoState
{
    public string RandomStateVal { get; set; } = Guid.NewGuid().ToString();
}
