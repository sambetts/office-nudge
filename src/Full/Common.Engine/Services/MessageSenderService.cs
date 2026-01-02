using Common.Engine.Storage;
using Microsoft.Extensions.Logging;

namespace Common.Engine.Services;

/// <summary>
/// Service responsible for sending messages to recipients (mocked for now)
/// </summary>
public class MessageSenderService
{
    private readonly MessageTemplateService _templateService;
    private readonly ILogger<MessageSenderService> _logger;

    public MessageSenderService(MessageTemplateService templateService, ILogger<MessageSenderService> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to a recipient (mocked - always succeeds)
    /// </summary>
    public async Task<MessageSendResult> SendMessageAsync(BatchQueueMessage queueMessage)
    {
        try
        {
            _logger.LogInformation($"Processing message for recipient {queueMessage.RecipientUpn} in batch {queueMessage.BatchId}");

            // Mock: Simulate message sending
            // In a real implementation, this would:
            // 1. Load the template from blob storage
            // 2. Personalize the message for the recipient
            // 3. Send via Teams Bot API or other channel
            // 4. Handle errors and retries

            // Simulate some processing time
            await Task.Delay(100);

            // Mock: Always succeed for now
            _logger.LogInformation($"Successfully sent message to {queueMessage.RecipientUpn}");

            // Update the message log status to Success
            await _templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Success");

            return new MessageSendResult
            {
                Success = true,
                MessageLogId = queueMessage.MessageLogId,
                RecipientUpn = queueMessage.RecipientUpn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to {queueMessage.RecipientUpn}");

            // Update the message log status to Failed
            await _templateService.UpdateMessageLogStatus(queueMessage.MessageLogId, "Failed", ex.Message);

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
