using Common.Engine.Notifications;
using Common.Engine.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Engine.Services;

/// <summary>
/// Service responsible for sending messages to recipients
/// </summary>
public class MessageSenderService(IBotConvoResumeManager botConvoResumeManager, IServiceProvider serviceProvider, ILogger<MessageSenderService> logger)
{

    /// <summary>
    /// Send a message to a recipient
    /// </summary>
    public async Task<MessageSendResult> SendMessageAsync(BatchQueueMessage queueMessage)
    {
        try
        {
            logger.LogInformation($"Processing message for recipient {queueMessage.RecipientUpn} in batch {queueMessage.BatchId}");

            // Send message via bot conversation
            var resumeResult = await botConvoResumeManager.ResumeConversation(queueMessage.RecipientUpn);

            // Create a scope to resolve scoped MessageTemplateService
            using var scope = serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<MessageTemplateService>();

            if (resumeResult.Success)
            {
                logger.LogInformation($"Successfully sent message to {queueMessage.RecipientUpn}: {resumeResult.Message}");

                // Update the message log status to Success
                await templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Success");

                return new MessageSendResult
                {
                    Success = true,
                    MessageLogId = queueMessage.MessageLogId,
                    RecipientUpn = queueMessage.RecipientUpn
                };
            }
            else
            {
                logger.LogWarning($"Failed to send message to {queueMessage.RecipientUpn}: {resumeResult.Message}");

                // Update the message log status to Failed
                await templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Failed", resumeResult.Message);

                return new MessageSendResult
                {
                    Success = false,
                    MessageLogId = queueMessage.MessageLogId,
                    RecipientUpn = queueMessage.RecipientUpn,
                    ErrorMessage = resumeResult.Message
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error sending message to {queueMessage.RecipientUpn}");

            // Create a scope to resolve scoped MessageTemplateService for error handling
            using var scope = serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<MessageTemplateService>();

            // Update the message log status to Failed
            await templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Failed", ex.Message);

            return new MessageSendResult
            {
                Success = false,
                MessageLogId = queueMessage.MessageLogId,
                RecipientUpn = queueMessage.RecipientUpn,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Result of a message send operation
/// </summary>
public class MessageSendResult
{
    public bool Success { get; set; }
    public string MessageLogId { get; set; } = null!;
    public string RecipientUpn { get; set; } = null!;
    public string? ErrorMessage { get; set; }
}
