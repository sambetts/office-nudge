using Common.Engine.Storage;
using Microsoft.Extensions.Logging;

namespace Common.Engine.Services;

/// <summary>
/// Service for managing message templates and logs
/// </summary>
public class MessageTemplateService
{
    private readonly MessageTemplateStorageManager _storageManager;
    private readonly ILogger<MessageTemplateService> _logger;

    public MessageTemplateService(MessageTemplateStorageManager storageManager, ILogger<MessageTemplateService> logger)
    {
        _storageManager = storageManager;
        _logger = logger;
    }

    public async Task<MessageTemplateDto> CreateTemplate(string templateName, string jsonPayload, string createdByUpn)
    {
        _logger.LogInformation($"Creating template '{templateName}' by {createdByUpn}");
        var entity = await _storageManager.SaveTemplate(templateName, jsonPayload, createdByUpn);
        return MapToDto(entity);
    }

    public async Task<List<MessageTemplateDto>> GetAllTemplates()
    {
        var entities = await _storageManager.GetAllTemplates();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<MessageTemplateDto?> GetTemplate(string templateId)
    {
        var entity = await _storageManager.GetTemplate(templateId);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<string> GetTemplateJson(string templateId)
    {
        return await _storageManager.GetTemplateJson(templateId);
    }

    public async Task<MessageTemplateDto> UpdateTemplate(string templateId, string templateName, string jsonPayload)
    {
        _logger.LogInformation($"Updating template {templateId}");
        var entity = await _storageManager.UpdateTemplate(templateId, templateName, jsonPayload);
        return MapToDto(entity);
    }

    public async Task DeleteTemplate(string templateId)
    {
        _logger.LogInformation($"Deleting template {templateId}");
        await _storageManager.DeleteTemplate(templateId);
    }

    public async Task<MessageLogDto> LogMessageSend(string templateId, string? recipientUpn, string status)
    {
        var entity = await _storageManager.LogMessageSend(templateId, recipientUpn, status);
        return MapLogToDto(entity);
    }

    public async Task<List<MessageLogDto>> GetAllMessageLogs()
    {
        var entities = await _storageManager.GetAllMessageLogs();
        return entities.Select(MapLogToDto).ToList();
    }

    public async Task<List<MessageLogDto>> GetMessageLogsByTemplate(string templateId)
    {
        var entities = await _storageManager.GetMessageLogsByTemplate(templateId);
        return entities.Select(MapLogToDto).ToList();
    }

    private MessageTemplateDto MapToDto(MessageTemplateTableEntity entity)
    {
        return new MessageTemplateDto
        {
            Id = entity.RowKey,
            TemplateName = entity.TemplateName,
            BlobUrl = entity.BlobUrl,
            CreatedByUpn = entity.CreatedByUpn,
            CreatedDate = entity.CreatedDate
        };
    }

    private MessageLogDto MapLogToDto(MessageLogTableEntity entity)
    {
        return new MessageLogDto
        {
            Id = entity.RowKey,
            TemplateId = entity.TemplateId,
            SentDate = entity.SentDate,
            RecipientUpn = entity.RecipientUpn,
            Status = entity.Status
        };
    }
}

public class MessageTemplateDto
{
    public string Id { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string BlobUrl { get; set; } = null!;
    public string CreatedByUpn { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
}

public class MessageLogDto
{
    public string Id { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public string? RecipientUpn { get; set; }
    public string Status { get; set; } = null!;
}
