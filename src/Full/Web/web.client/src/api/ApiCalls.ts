import { ServiceConfiguration, MessageTemplateDto, MessageLogDto, CreateTemplateRequest, UpdateTemplateRequest, LogMessageSendRequest } from "../apimodels/Models";
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

export const logMessageSend = async (loader: BaseAxiosApiLoader, request: LogMessageSendRequest): Promise<MessageLogDto> => {
  return loader.loadFromApi('api/MessageTemplate/LogSend', 'POST', request);
}

export const getAllMessageLogs = async (loader: BaseAxiosApiLoader): Promise<MessageLogDto[]> => {
  return loader.loadFromApi('api/MessageTemplate/GetLogs', 'GET');
}

export const getMessageLogsByTemplate = async (loader: BaseAxiosApiLoader, templateId: string): Promise<MessageLogDto[]> => {
  return loader.loadFromApi(`api/MessageTemplate/GetLogsByTemplate/${templateId}`, 'GET');
}

