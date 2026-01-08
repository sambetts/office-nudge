using Azure.Identity;
using Common.Engine.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Common.Engine.Services;

/// <summary>
/// Extended user information with metadata for AI-driven user matching
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
    /// Creates a summary string for AI processing
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

/// <summary>
/// Service for loading users from Microsoft Graph with extended metadata.
/// Used for AI-driven smart group resolution.
/// </summary>
public class GraphUserService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphUserService> _logger;

    // Properties to request for enriched user data
    private static readonly string[] UserSelectProperties =
    [
        "id",
        "userPrincipalName",
        "displayName",
        "givenName",
        "surname",
        "mail",
        "department",
        "jobTitle",
        "officeLocation",
        "city",
        "country",
        "state",
        "companyName",
        "employeeType",
        "employeeHireDate"
    ];

    public GraphUserService(AzureADAuthConfig config, ILogger<GraphUserService> logger)
    {
        _logger = logger;

        var clientSecretCredential = new ClientSecretCredential(
            config.TenantId,
            config.ClientId,
            config.ClientSecret);

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        _graphClient = new GraphServiceClient(clientSecretCredential, scopes);
    }

    /// <summary>
    /// Get all users from the tenant with extended metadata.
    /// Note: For large tenants, consider implementing paging and filtering.
    /// </summary>
    /// <param name="maxUsers">Maximum number of users to retrieve (default 999)</param>
    public async Task<List<EnrichedUserInfo>> GetAllUsersWithMetadataAsync(int maxUsers = 999)
    {
        var users = new List<EnrichedUserInfo>();

        try
        {
            _logger.LogInformation("Fetching users with extended metadata from Graph...");

            var result = await _graphClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = UserSelectProperties;
                requestConfiguration.QueryParameters.Top = Math.Min(maxUsers, 999);
                requestConfiguration.QueryParameters.Filter = "accountEnabled eq true and userType eq 'Member'";
                requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            });

            if (result?.Value != null)
            {
                foreach (var user in result.Value)
                {
                    var enrichedUser = MapToEnrichedUser(user);
                    users.Add(enrichedUser);
                }
            }

            // Handle paging for large tenants
            var pageIterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(
                _graphClient,
                result!,
                user =>
                {
                    if (users.Count >= maxUsers)
                        return false;

                    users.Add(MapToEnrichedUser(user));
                    return true;
                });

            await pageIterator.IterateAsync();

            _logger.LogInformation($"Retrieved {users.Count} users with metadata from Graph");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with metadata from Graph");
            throw;
        }

        return users;
    }

    /// <summary>
    /// Get users filtered by department
    /// </summary>
    public async Task<List<EnrichedUserInfo>> GetUsersByDepartmentAsync(string department)
    {
        var users = new List<EnrichedUserInfo>();

        try
        {
            _logger.LogInformation($"Fetching users in department '{department}' from Graph...");

            var result = await _graphClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = UserSelectProperties;
                requestConfiguration.QueryParameters.Filter = $"accountEnabled eq true and department eq '{department}'";
                requestConfiguration.QueryParameters.Top = 999;
                requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            });

            if (result?.Value != null)
            {
                users.AddRange(result.Value.Select(MapToEnrichedUser));
            }

            _logger.LogInformation($"Retrieved {users.Count} users in department '{department}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting users by department from Graph");
            throw;
        }

        return users;
    }

    /// <summary>
    /// Get a single user with extended metadata
    /// </summary>
    public async Task<EnrichedUserInfo?> GetUserWithMetadataAsync(string upn)
    {
        try
        {
            var user = await _graphClient.Users[upn].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = UserSelectProperties;
            });

            if (user != null)
            {
                var enrichedUser = MapToEnrichedUser(user);
                
                // Try to get manager info
                try
                {
                    var manager = await _graphClient.Users[upn].Manager.GetAsync();
                    if (manager is User managerUser)
                    {
                        enrichedUser.ManagerUpn = managerUser.UserPrincipalName;
                        enrichedUser.ManagerDisplayName = managerUser.DisplayName;
                    }
                }
                catch
                {
                    // Manager not found or not accessible - continue without it
                }

                return enrichedUser;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user {upn} with metadata from Graph");
            return null;
        }
    }

    /// <summary>
    /// Get managers for batch of users (for enrichment)
    /// </summary>
    public async Task EnrichUsersWithManagersAsync(List<EnrichedUserInfo> users)
    {
        _logger.LogInformation($"Enriching {users.Count} users with manager information...");
        
        var tasks = users.Select(async user =>
        {
            try
            {
                var manager = await _graphClient.Users[user.UserPrincipalName].Manager.GetAsync();
                if (manager is User managerUser)
                {
                    user.ManagerUpn = managerUser.UserPrincipalName;
                    user.ManagerDisplayName = managerUser.DisplayName;
                }
            }
            catch
            {
                // Manager not found or not accessible - continue
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Manager enrichment completed");
    }

    private static EnrichedUserInfo MapToEnrichedUser(User user)
    {
        return new EnrichedUserInfo
        {
            Id = user.Id ?? string.Empty,
            UserPrincipalName = user.UserPrincipalName ?? string.Empty,
            DisplayName = user.DisplayName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            Mail = user.Mail,
            Department = user.Department,
            JobTitle = user.JobTitle,
            OfficeLocation = user.OfficeLocation,
            City = user.City,
            Country = user.Country,
            State = user.State,
            CompanyName = user.CompanyName,
            EmployeeType = user.EmployeeType,
            HireDate = user.EmployeeHireDate
        };
    }
}
