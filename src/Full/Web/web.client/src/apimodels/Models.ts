export interface BaseDTO { id?: string }

export interface ServiceConfiguration {
  storageInfo: StorageInfo;
}

export interface StorageInfo {
  accountURI: string;
  sharedAccessToken: string;
}

export interface MessageTemplateDto {
  id: string;
  templateName: string;
  blobUrl: string;
  createdByUpn: string;
  createdDate: string;
}

export interface MessageBatchDto {
  id: string;
  batchName: string;
  templateId: string;
  senderUpn: string;
  createdDate: string;
}

export interface MessageLogDto {
  id: string;
  messageBatchId: string;
  sentDate: string;
  recipientUpn?: string;
  status: string;
  lastError?: string;
}

export interface CreateTemplateRequest {
  templateName: string;
  jsonPayload: string;
}

export interface UpdateTemplateRequest {
  templateName: string;
  jsonPayload: string;
}

export interface CreateBatchAndSendRequest {
  batchName: string;
  templateId: string;
  recipientUpns: string[];
}

export interface UpdateLogStatusRequest {
  status: string;
  lastError?: string;
}

export interface ParseFileResponse {
  upns: string[];
}

export interface MessageStatusStatsDto {
  sentCount: number;
  failedCount: number;
  pendingCount: number;
  totalCount: number;
}

export interface UserCoverageStatsDto {
  usersMessaged: number;
  totalUsersInTenant: number;
  usersNotMessaged: number;
  coveragePercentage: number;
}

export interface QueueStatusDto {
  success: boolean;
  queueLength: number;
  timestamp: string;
}




