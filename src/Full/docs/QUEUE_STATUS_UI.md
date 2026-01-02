# Queue Status in Batch Progress Page

## Overview

The Batch Progress page now displays real-time queue status information, showing users how many messages are waiting to be processed in the Azure Storage Queue.

## Changes Made

### Backend
- **DiagnosticsController** already had the `QueueStatus` endpoint that returns the current queue length

### Frontend

#### 1. Models (`Web/web.client/src/apimodels/Models.ts`)
Added new interface:
```typescript
export interface QueueStatusDto {
  success: boolean;
  queueLength: number;
  timestamp: string;
}
```

#### 2. API Calls (`Web/web.client/src/api/ApiCalls.ts`)
Added new API call:
```typescript
export const getQueueStatus = async (loader: BaseAxiosApiLoader): Promise<QueueStatusDto> => {
  return loader.loadFromApi('api/Diagnostics/QueueStatus', 'GET');
}
```

#### 3. Batch Progress Page (`Web/web.client/src/pages/BatchProgress/BatchProgressPage.tsx`)

**New Features:**
- Fetches queue status alongside batch and log data
- Displays a prominent queue status indicator when messages are in the queue
- Shows "X messages in queue" with a queue icon
- Auto-refreshes every 5 seconds when there are pending messages or items in queue
- Stops auto-refresh when all messages are processed AND queue is empty
- Properly handles "Success" status (in addition to "Sent")

**Visual Design:**
- Queue status displayed in a highlighted section within the Batch Information card
- Uses Queue icon from Fluent UI
- Shows helpful message: "Waiting to be processed by background service"
- Only visible when queue length > 0

## User Experience

1. **After creating a batch:**
   - User clicks "View Batch Progress"
   - Batch page loads showing initial state with all messages as "Pending"
   - Queue status indicator shows: "X messages in queue"

2. **During processing:**
   - Page auto-refreshes every 5 seconds
   - Queue count decreases as messages are processed
   - Message statuses update from "Pending" to "Success"
   - Progress bar updates in real-time

3. **After completion:**
   - All messages show "Success" status
   - Queue shows 0 messages
   - Auto-refresh stops
   - 100% progress achieved

## Benefits

? **Transparency** - Users can see the queue backlog at a glance  
? **Real-time Updates** - Auto-refresh keeps information current  
? **Better UX** - Clear indication that messages are being processed  
? **Performance Insight** - Can see if queue is growing or processing normally  

## Example Display

```
???????????????????????????????????????????
? Batch Information                       ?
???????????????????????????????????????????
? Batch ID: abc-123                       ?
? Template: Weekly Reminder               ?
? Sender: admin@example.com               ?
? Created: 1/15/2024, 10:30:00 AM        ?
?                                         ?
? ?? 42 messages in queue                 ?
?    Waiting to be processed by           ?
?    background service                   ?
???????????????????????????????????????????
```

## API Endpoint Used

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
