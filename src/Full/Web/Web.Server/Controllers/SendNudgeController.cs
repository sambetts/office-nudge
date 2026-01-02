using Common.Engine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Web.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SendNudgeController : ControllerBase
{
    private readonly MessageTemplateService _templateService;
    private readonly ILogger<SendNudgeController> _logger;

    public SendNudgeController(MessageTemplateService templateService, ILogger<SendNudgeController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    // POST: api/SendNudge/ParseFile
    [HttpPost(nameof(ParseFile))]
    public async Task<IActionResult> ParseFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            var upns = new List<string>();
            
            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // Handle CSV - take first column if comma-separated
                        var upn = trimmedLine.Split(',')[0].Trim();
                        if (!string.IsNullOrWhiteSpace(upn))
                        {
                            upns.Add(upn);
                        }
                    }
                }
            }

            _logger.LogInformation($"Parsed {upns.Count} UPNs from file {file.FileName}");
            return Ok(new { upns });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file");
            return StatusCode(500, "Error parsing file");
        }
    }

    // POST: api/SendNudge/CreateBatchAndSend
    [HttpPost(nameof(CreateBatchAndSend))]
    public async Task<IActionResult> CreateBatchAndSend([FromBody] CreateBatchAndSendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BatchName))
        {
            return BadRequest("BatchName is required");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            return BadRequest("TemplateId is required");
        }

        if (request.RecipientUpns == null || !request.RecipientUpns.Any())
        {
            return BadRequest("At least one recipient UPN is required");
        }

        try
        {
            // Get user principal name from claims
            var senderUpn = User.Identity?.Name ?? "unknown";

            // Verify template exists
            var template = await _templateService.GetTemplate(request.TemplateId);
            if (template == null)
            {
                return NotFound($"Template {request.TemplateId} not found");
            }

            // Create batch
            var batch = await _templateService.CreateBatch(request.BatchName, request.TemplateId, senderUpn);

            // Create message log entries for each recipient
            var logs = await _templateService.LogBatchMessages(batch.Id, request.RecipientUpns);

            _logger.LogInformation($"Created batch {batch.Id} with {logs.Count} messages");

            return Ok(new
            {
                batch,
                messageCount = logs.Count,
                logs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch and sending messages");
            return StatusCode(500, "Error creating batch and sending messages");
        }
    }

    // PUT: api/SendNudge/UpdateLogStatus/{logId}
    [HttpPut("UpdateLogStatus/{logId}")]
    public async Task<IActionResult> UpdateLogStatus(string logId, [FromBody] UpdateLogStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest("Status is required");
        }

        try
        {
            await _templateService.UpdateMessageLogStatus(logId, request.Status, request.LastError);
            _logger.LogInformation($"Updated message log {logId} to status {request.Status}");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating message log status");
            return StatusCode(500, "Error updating message log status");
        }
    }
}

public class CreateBatchAndSendRequest
{
    public string BatchName { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
    public List<string> RecipientUpns { get; set; } = null!;
}

public class UpdateLogStatusRequest
{
    public string Status { get; set; } = null!;
    public string? LastError { get; set; }
}
