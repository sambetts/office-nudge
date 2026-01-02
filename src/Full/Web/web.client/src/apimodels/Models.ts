

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

export interface MessageLogDto {
  id: string;
  templateId: string;
  sentDate: string;
  recipientUpn?: string;
  status: string;
}

export interface CreateTemplateRequest {
  templateName: string;
  jsonPayload: string;
}

export interface UpdateTemplateRequest {
  templateName: string;
  jsonPayload: string;
}

export interface LogMessageSendRequest {
  templateId: string;
  recipientUpn?: string;
  status: string;
}




