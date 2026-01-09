using Azure.Data.Tables;
using Common.Engine.Storage;
using Microsoft.Extensions.Logging;

using Common.Engine.Models;

using Common.Engine.Config;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// Handles Copilot usage statistics retrieval and storage.
/// </summary>
internal class CopilotStatsService
{
    private readonly ILogger _logger;
    private readonly UserCacheConfig _config;

    public CopilotStatsService(ILogger logger, UserCacheConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Update cached users with Copilot statistics.
    /// </summary>
    public async Task UpdateCachedUsersWithStatsAsync(TableClient tableClient, List<CopilotUsageRecord> stats)
    {
        var updateCount = 0;

        foreach (var stat in stats)
        {
            try
            {
                var cachedUser = await tableClient.GetEntityAsync<UserCacheTableEntity>(
                    UserCacheTableEntity.PartitionKeyVal,
                    stat.UserPrincipalName);

                if (cachedUser.Value != null)
                {
                    var user = cachedUser.Value;
                    user.CopilotLastActivityDate = stat.LastActivityDate;
                    user.CopilotChatLastActivityDate = stat.CopilotChatLastActivityDate;
                    user.TeamscopilotLastActivityDate = stat.TeamsCopilotLastActivityDate;
                    user.WordCopilotLastActivityDate = stat.WordCopilotLastActivityDate;
                    user.ExcelCopilotLastActivityDate = stat.ExcelCopilotLastActivityDate;
                    user.PowerPointCopilotLastActivityDate = stat.PowerPointCopilotLastActivityDate;
                    user.OutlookCopilotLastActivityDate = stat.OutlookCopilotLastActivityDate;
                    user.OneNoteCopilotLastActivityDate = stat.OneNoteCopilotLastActivityDate;
                    user.LoopCopilotLastActivityDate = stat.LoopCopilotLastActivityDate;
                    user.LastCopilotStatsUpdate = DateTime.UtcNow;

                    await tableClient.UpdateEntityAsync(user, user.ETag, TableUpdateMode.Replace);
                    updateCount++;
                }
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogDebug($"User {stat.UserPrincipalName} not found in cache, skipping stats update");
            }
        }

        _logger.LogInformation($"Updated Copilot stats for {updateCount} users");
    }

    /// <summary>
    /// Fetch Copilot usage stats from Graph API.
    /// </summary>
    public async Task<List<CopilotUsageRecord>> GetCopilotUsageStatsAsync()
    {
        var records = new List<CopilotUsageRecord>();

        try
        {
            var requestUrl = $"https://graph.microsoft.com/beta/reports/getMicrosoft365CopilotUsageUserDetail(period='{_config.CopilotStatsPeriod}')?$format=text/csv";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + await GetAccessTokenAsync());

            var response = await httpClient.GetAsync(requestUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.Found)
            {
                var downloadUrl = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    var csvResponse = await httpClient.GetAsync(downloadUrl);
                    var csvContent = await csvResponse.Content.ReadAsStringAsync();
                    records = ParseCopilotUsageCsv(csvContent);
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                var csvContent = await response.Content.ReadAsStringAsync();
                records = ParseCopilotUsageCsv(csvContent);
            }
            else
            {
                _logger.LogWarning($"Copilot stats API returned {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Copilot usage stats");
            throw;
        }

        return records;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        throw new NotImplementedException(
            "GetAccessTokenAsync requires proper authentication setup. " +
            "Consider using Microsoft.Graph.Beta SDK which has built-in support for the Copilot reports API, " +
            "or implement a custom HTTP client with the same authentication as GraphServiceClient.");
    }

    private List<CopilotUsageRecord> ParseCopilotUsageCsv(string csvContent)
    {
        var records = new List<CopilotUsageRecord>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            return records;
        }

        var columnIndices = ParseCsvHeader(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length <= columnIndices.UpnIndex)
                continue;

            var record = new CopilotUsageRecord
            {
                UserPrincipalName = values[columnIndices.UpnIndex],
                LastActivityDate = ParseCsvDate(values, columnIndices.LastActivityIndex),
                CopilotChatLastActivityDate = ParseCsvDate(values, columnIndices.CopilotChatIndex),
                TeamsCopilotLastActivityDate = ParseCsvDate(values, columnIndices.TeamsIndex),
                WordCopilotLastActivityDate = ParseCsvDate(values, columnIndices.WordIndex),
                ExcelCopilotLastActivityDate = ParseCsvDate(values, columnIndices.ExcelIndex),
                PowerPointCopilotLastActivityDate = ParseCsvDate(values, columnIndices.PowerPointIndex),
                OutlookCopilotLastActivityDate = ParseCsvDate(values, columnIndices.OutlookIndex),
                OneNoteCopilotLastActivityDate = ParseCsvDate(values, columnIndices.OneNoteIndex),
                LoopCopilotLastActivityDate = ParseCsvDate(values, columnIndices.LoopIndex)
            };

            records.Add(record);
        }

        return records;
    }

    private CsvColumnIndices ParseCsvHeader(string headerLine)
    {
        var headers = headerLine.Split(',');

        return new CsvColumnIndices
        {
            UpnIndex = Array.IndexOf(headers, "User Principal Name"),
            LastActivityIndex = Array.IndexOf(headers, "Last Activity Date"),
            CopilotChatIndex = Array.IndexOf(headers, "Copilot Chat Last Activity Date"),
            TeamsIndex = Array.IndexOf(headers, "Microsoft Teams Copilot Last Activity Date"),
            WordIndex = Array.IndexOf(headers, "Word Copilot Last Activity Date"),
            ExcelIndex = Array.IndexOf(headers, "Excel Copilot Last Activity Date"),
            PowerPointIndex = Array.IndexOf(headers, "PowerPoint Copilot Last Activity Date"),
            OutlookIndex = Array.IndexOf(headers, "Outlook Copilot Last Activity Date"),
            OneNoteIndex = Array.IndexOf(headers, "OneNote Copilot Last Activity Date"),
            LoopIndex = Array.IndexOf(headers, "Loop Copilot Last Activity Date")
        };
    }

    private DateTime? ParseCsvDate(string[] values, int index)
    {
        if (index < 0 || index >= values.Length)
            return null;

        var value = values[index].Trim();
        if (string.IsNullOrEmpty(value))
            return null;

        return DateTime.TryParse(value, out var date) ? date : null;
    }
}

