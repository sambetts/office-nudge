import { ServiceConfiguration, MessageTemplateDto, MessageLogDto, CreateTemplateRequest, UpdateTemplateRequest, MessageBatchDto, CreateBatchAndSendRequest, UpdateLogStatusRequest, ParseFileResponse, MessageStatusStatsDto, UserCoverageStatsDto } from "../apimodels/Models";
import { BaseAxiosApiLoader } from "./AxiosApiLoader";


export const getClientConfig = async (loader: BaseAxiosApiLoader): Promise<ServiceConfiguration> => {
  return loader.loadFromApi('api/AppInfo/GetClientConfig', 'POST');
}

// Message Template API calls
export const getAllTemplates = async (loader: BaseAxiosApiLoader): Promise<MessageTemplateDto[]> => {
  return loader.loadFromApi('api/MessageTemplate/GetAll', 'GET');
}

export const getTemplate = async (loader: BaseAxiosApiLoader, id: string): Promise<MessageTemplateDto> => {
  return loader.loadFromApi(`api/MessageTemplate/Get/${id}`, 'GET');
}

export const getTemplateJson = async (loader: BaseAxiosApiLoader, id: string): Promise<{ json: string }> => {
  return loader.loadFromApi(`api/MessageTemplate/GetJson/${id}`, 'GET');
}

export const createTemplate = async (loader: BaseAxiosApiLoader, request: CreateTemplateRequest): Promise<MessageTemplateDto> => {
  return loader.loadFromApi('api/MessageTemplate/Create', 'POST', request);
}

export const updateTemplate = async (loader: BaseAxiosApiLoader, id: string, request: UpdateTemplateRequest): Promise<MessageTemplateDto> => {
  return loader.loadFromApi(`api/MessageTemplate/Update/${id}`, 'PUT', request);
}

export const deleteTemplate = async (loader: BaseAxiosApiLoader, id: string): Promise<void> => {
  return loader.loadFromApi(`api/MessageTemplate/Delete/${id}`, 'DELETE');
}

export const getAllMessageLogs = async (loader: BaseAxiosApiLoader): Promise<MessageLogDto[]> => {
  return loader.loadFromApi('api/MessageTemplate/GetLogs', 'GET');
}

export const getMessageLogsByTemplate = async (loader: BaseAxiosApiLoader, templateId: string): Promise<MessageLogDto[]> => {
  return loader.loadFromApi(`api/MessageTemplate/GetLogsByTemplate/${templateId}`, 'GET');
}

export const getMessageLogsByBatch = async (loader: BaseAxiosApiLoader, batchId: string): Promise<MessageLogDto[]> => {
  return loader.loadFromApi(`api/MessageTemplate/GetLogsByBatch/${batchId}`, 'GET');
}

export const getAllBatches = async (loader: BaseAxiosApiLoader): Promise<MessageBatchDto[]> => {
  return loader.loadFromApi('api/MessageTemplate/GetBatches', 'GET');
}

export const getBatch = async (loader: BaseAxiosApiLoader, id: string): Promise<MessageBatchDto> => {
  return loader.loadFromApi(`api/MessageTemplate/GetBatch/${id}`, 'GET');
}

// Send Nudge API calls
export const parseFile = async (loader: BaseAxiosApiLoader, file: File): Promise<ParseFileResponse> => {
  const formData = new FormData();
  formData.append('file', file);
  
  return loader.loadFromApiWithFormData('api/SendNudge/ParseFile', 'POST', formData);
}

export const createBatchAndSend = async (loader: BaseAxiosApiLoader, request: CreateBatchAndSendRequest): Promise<any> => {
  return loader.loadFromApi('api/SendNudge/CreateBatchAndSend', 'POST', request);
}

export const updateLogStatus = async (loader: BaseAxiosApiLoader, logId: string, request: UpdateLogStatusRequest): Promise<void> => {
  return loader.loadFromApi(`api/SendNudge/UpdateLogStatus/${logId}`, 'PUT', request);
}

// Statistics API calls
export const getMessageStatusStats = async (loader: BaseAxiosApiLoader): Promise<MessageStatusStatsDto> => {
  return loader.loadFromApi('api/Statistics/GetMessageStatusStats', 'GET');
}

export const getUserCoverageStats = async (loader: BaseAxiosApiLoader): Promise<UserCoverageStatsDto> => {
  return loader.loadFromApi('api/Statistics/GetUserCoverageStats', 'GET');
}

