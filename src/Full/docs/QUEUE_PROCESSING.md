# Queue-Based Batch Message Processing

## Overview

This implementation provides asynchronous, queue-based processing for batch messages. When a new message batch is created, each recipient message is queued for processing by a background service.

## Architecture

### Components

1. **BatchQueueMessage** (`Common.Engine/Storage/BatchQueueMessage.cs`)
   - Data model for messages queued for processing
   - Contains batch ID, message log ID, recipient UPN, and template ID

2. **BatchQueueService** (`Common.Engine/Services/BatchQueueService.cs`)
   - Manages Azure Storage Queue operations
   - Handles enqueueing and dequeueing messages
   - Provides queue status monitoring

3. **MessageSenderService** (`Common.Engine/Services/MessageSenderService.cs`)
   - Processes individual messages (currently mocked)
   - Updates message log status to "Success" or "Failed"
   - In production, would integrate with Teams Bot API or other messaging channels

4. **BatchMessageProcessorService** (`Common.Engine/BackgroundServices/BatchMessageProcessorService.cs`)
   - Background service that continuously polls the queue
   - Processes messages asynchronously
   - Handles errors and logging

## Flow

1. **Batch Creation**: When `CreateBatchAndSend` is called via the API:
   - A new batch record is created in Azure Table Storage
   - Message log entries are created for each recipient with status "Pending"
   - Each message is enqueued to Azure Storage Queue

2. **Background Processing**: The `BatchMessageProcessorService` continuously:
   - Polls the queue every 5 seconds
   - Dequeues available messages
   - Calls `MessageSenderService.SendMessageAsync()` to process the message
   - Updates the message log status to "Success" (currently mocked)
   - Deletes the processed message from the queue

3. **Status Tracking**: Message status can be monitored through:
   - Message log entries in Azure Table Storage
   - Queue status endpoint: `GET /api/Diagnostics/QueueStatus`
   - Statistics endpoint: `GET /api/Statistics/GetMessageStatusStats`

## Configuration

The queue uses the same Azure Storage connection string as other storage services:

```json
{
  "ConnectionStrings": {
    "Storage": "UseDevelopmentStorage=true"
  }
}
```

Queue name: `batch-messages`

## Mock Implementation

Currently, the `MessageSenderService` is mocked to:
- Simulate 100ms processing time
- Always succeed (set status to "Success")
- Log each message send operation

To implement actual message sending:
1. Load the message template from blob storage using the `TemplateId`
2. Personalize the message for the recipient
3. Send via Teams Bot API or other messaging channel
4. Handle retries and errors appropriately

## Monitoring

### Queue Status
Check the current queue length:
```
GET /api/Diagnostics/QueueStatus
```

Response:
```json
{
  "success": true,
  "queueLength": 42,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Message Log Status
View message processing status via:
```
GET /api/Statistics/GetMessageStatusStats
```

## Benefits of Queue-Based Approach

1. **Asynchronous Processing**: API responds immediately without waiting for all messages to be sent
2. **Scalability**: Can process messages in parallel (future enhancement)
3. **Reliability**: Messages are persisted in queue until successfully processed
4. **Monitoring**: Queue depth provides insight into processing backlog
5. **Decoupling**: Message creation and sending are independent processes

## Future Enhancements

1. **Dead Letter Queue**: Handle messages that fail repeatedly
2. **Parallel Processing**: Process multiple messages concurrently
3. **Priority Queue**: Prioritize certain batches or recipients
4. **Retry Logic**: Implement exponential backoff for failed sends
5. **Real Message Sending**: Integrate with Teams Bot API or other channels
