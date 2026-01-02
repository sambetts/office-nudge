using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Common.Engine.Storage;
using Microsoft.Extensions.Logging;

namespace Common.Engine;

/// <summary>
/// Manages message templates in Azure Storage (Table + Blob)
/// </summary>
public class MessageTemplateStorageManager : TableStorageManager
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger _logger;
    private const string TEMPLATES_TABLE_NAME = "messagetemplates";
    private const string LOGS_TABLE_NAME = "messagelogs";
    private const string BLOB_CONTAINER_NAME = "message-templates";

    public MessageTemplateStorageManager(string storageConnectionString, ILogger logger) 
        : base(storageConnectionString)
    {
        _blobServiceClient = new BlobServiceClient(storageConnectionString);
        _logger = logger;
    }

    #region Template Management

    /// <summary>
    /// Save a message template to blob storage and create table entry
    /// </summary>
    public async Task<MessageTemplateTableEntity> SaveTemplate(string templateName, string jsonPayload, string createdByUpn)
    {
        var templateId = Guid.NewGuid().ToString();
        
        // Save JSON to blob storage
        var blobUrl = await SaveTemplateToBlobStorage(templateId, jsonPayload);

        // Create table entry with blob reference
        var tableEntity = new MessageTemplateTableEntity
        {
            RowKey = templateId,
            TemplateName = templateName,
            BlobUrl = blobUrl,
            CreatedByUpn = createdByUpn,
            CreatedDate = DateTime.UtcNow
        };

        var tableClient = await GetTableClient(TEMPLATES_TABLE_NAME);
        await tableClient.AddEntityAsync(tableEntity);

        _logger.LogInformation($"Saved template '{templateName}' with ID {templateId}");
        return tableEntity;
    }

    /// <summary>
    /// Get all message templates
    /// </summary>
    public async Task<List<MessageTemplateTableEntity>> GetAllTemplates()
    {
        var tableClient = await GetTableClient(TEMPLATES_TABLE_NAME);
        var templates = new List<MessageTemplateTableEntity>();

        await foreach (var entity in tableClient.QueryAsync<MessageTemplateTableEntity>(
            filter: $"PartitionKey eq '{MessageTemplateTableEntity.PartitionKeyVal}'"))
        {
            templates.Add(entity);
        }

        return templates;
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    public async Task<MessageTemplateTableEntity?> GetTemplate(string templateId)
    {
        var tableClient = await GetTableClient(TEMPLATES_TABLE_NAME);
        try
        {
            var response = await tableClient.GetEntityAsync<MessageTemplateTableEntity>(
                MessageTemplateTableEntity.PartitionKeyVal, templateId);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Get the JSON content from blob storage
    /// </summary>
    public async Task<string> GetTemplateJson(string templateId)
    {
        var template = await GetTemplate(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var blobClient = new BlobClient(new Uri(template.BlobUrl));
        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    /// <summary>
    /// Update a template
    /// </summary>
    public async Task<MessageTemplateTableEntity> UpdateTemplate(string templateId, string templateName, string jsonPayload)
    {
        var template = await GetTemplate(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        // Update blob content
        var blobUrl = await SaveTemplateToBlobStorage(templateId, jsonPayload);

        // Update table entry
        template.TemplateName = templateName;
        template.BlobUrl = blobUrl;

        var tableClient = await GetTableClient(TEMPLATES_TABLE_NAME);
        await tableClient.UpdateEntityAsync(template, template.ETag, TableUpdateMode.Replace);

        _logger.LogInformation($"Updated template {templateId}");
        return template;
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    public async Task DeleteTemplate(string templateId)
    {
        var template = await GetTemplate(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        // Delete blob
        var blobClient = new BlobClient(new Uri(template.BlobUrl));
        await blobClient.DeleteIfExistsAsync();

        // Delete table entry
        var tableClient = await GetTableClient(TEMPLATES_TABLE_NAME);
        await tableClient.DeleteEntityAsync(MessageTemplateTableEntity.PartitionKeyVal, templateId);

        _logger.LogInformation($"Deleted template {templateId}");
    }

    private async Task<string> SaveTemplateToBlobStorage(string templateId, string jsonPayload)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(BLOB_CONTAINER_NAME);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{templateId}.json";
        var blobClient = containerClient.GetBlobClient(blobName);

        var content = BinaryData.FromString(jsonPayload);
        await blobClient.UploadAsync(content, overwrite: true);

        return blobClient.Uri.ToString();
    }

    #endregion

    #region Message Logs

    /// <summary>
    /// Log a message send event
    /// </summary>
    public async Task<MessageLogTableEntity> LogMessageSend(string templateId, string? recipientUpn, string status)
    {
        var logEntity = new MessageLogTableEntity
        {
            RowKey = Guid.NewGuid().ToString(),
            TemplateId = templateId,
            SentDate = DateTime.UtcNow,
            RecipientUpn = recipientUpn,
            Status = status
        };

        var tableClient = await GetTableClient(LOGS_TABLE_NAME);
        await tableClient.AddEntityAsync(logEntity);

        _logger.LogInformation($"Logged message send for template {templateId}");
        return logEntity;
    }

    /// <summary>
    /// Get all message logs
    /// </summary>
    public async Task<List<MessageLogTableEntity>> GetAllMessageLogs()
    {
        var tableClient = await GetTableClient(LOGS_TABLE_NAME);
        var logs = new List<MessageLogTableEntity>();

        await foreach (var entity in tableClient.QueryAsync<MessageLogTableEntity>(
            filter: $"PartitionKey eq '{MessageLogTableEntity.PartitionKeyVal}'"))
        {
            logs.Add(entity);
        }

        return logs;
    }

    /// <summary>
    /// Get message logs for a specific template
    /// </summary>
    public async Task<List<MessageLogTableEntity>> GetMessageLogsByTemplate(string templateId)
    {
        var tableClient = await GetTableClient(LOGS_TABLE_NAME);
        var logs = new List<MessageLogTableEntity>();

        await foreach (var entity in tableClient.QueryAsync<MessageLogTableEntity>(
            filter: $"PartitionKey eq '{MessageLogTableEntity.PartitionKeyVal}' and TemplateId eq '{templateId}'"))
        {
            logs.Add(entity);
        }

        return logs;
    }

    #endregion
}
