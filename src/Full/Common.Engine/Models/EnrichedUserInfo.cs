namespace Common.Engine.Models;

/// <summary>
/// Extended user information with metadata for AI-driven user matching.
/// </summary>
public class EnrichedUserInfo
{
    public string Id { get; set; } = null!;
    public string UserPrincipalName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Mail { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? OfficeLocation { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? CompanyName { get; set; }
    public string? ManagerUpn { get; set; }
    public string? ManagerDisplayName { get; set; }
    public string? EmployeeType { get; set; }
    public DateTimeOffset? HireDate { get; set; }

    /// <summary>
    /// Indicates if the user has been deleted (used in delta queries).
    /// </summary>
    public bool IsDeleted { get; set; }

    // Copilot usage statistics
    public DateTime? CopilotLastActivityDate { get; set; }
    public DateTime? CopilotChatLastActivityDate { get; set; }
    public DateTime? TeamsCopilotLastActivityDate { get; set; }
    public DateTime? WordCopilotLastActivityDate { get; set; }
    public DateTime? ExcelCopilotLastActivityDate { get; set; }
    public DateTime? PowerPointCopilotLastActivityDate { get; set; }
    public DateTime? OutlookCopilotLastActivityDate { get; set; }
    public DateTime? OneNoteCopilotLastActivityDate { get; set; }
    public DateTime? LoopCopilotLastActivityDate { get; set; }
    public DateTime? LastCopilotStatsUpdate { get; set; }

    /// <summary>
    /// Creates a summary string for AI processing.
    /// </summary>
    public string ToAISummary()
    {
        var parts = new List<string>
        {
            $"UPN: {UserPrincipalName}",
            $"Name: {DisplayName ?? "Unknown"}"
        };

        if (!string.IsNullOrEmpty(JobTitle))
            parts.Add($"Job Title: {JobTitle}");
        if (!string.IsNullOrEmpty(Department))
            parts.Add($"Department: {Department}");
        if (!string.IsNullOrEmpty(OfficeLocation))
            parts.Add($"Office: {OfficeLocation}");
        if (!string.IsNullOrEmpty(City))
            parts.Add($"City: {City}");
        if (!string.IsNullOrEmpty(State))
            parts.Add($"State: {State}");
        if (!string.IsNullOrEmpty(Country))
            parts.Add($"Country: {Country}");
        if (!string.IsNullOrEmpty(CompanyName))
            parts.Add($"Company: {CompanyName}");
        if (!string.IsNullOrEmpty(ManagerDisplayName))
            parts.Add($"Manager: {ManagerDisplayName}");
        if (!string.IsNullOrEmpty(EmployeeType))
            parts.Add($"Employee Type: {EmployeeType}");

        return string.Join(" | ", parts);
    }
}
