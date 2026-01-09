using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

using Common.Engine.Models;

using Common.Engine.Config;

namespace Common.Engine.Services.UserCache;

/// <summary>
/// Handles Microsoft Graph delta query operations for user synchronization.
/// </summary>
internal class DeltaQueryService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger _logger;
    private readonly string[] _userSelectProperties;

    public DeltaQueryService(
        GraphServiceClient graphClient,
        ILogger logger,
        string[] userSelectProperties)
    {
        _graphClient = graphClient;
        _logger = logger;
        _userSelectProperties = userSelectProperties;
    }

    /// <summary>
    /// Fetch all users via initial delta query.
    /// </summary>
    public async Task<DeltaQueryResult> FetchAllUsersAsync()
    {
        var usersToProcess = new List<User>();

        // Initial delta query request
        // Note: Delta queries do NOT support $filter parameter
        var deltaRequest = await _graphClient.Users.Delta.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Select = _userSelectProperties;
            // Removed filter - not supported by delta queries
        });

        // Collect first page
        if (deltaRequest?.Value != null)
        {
            // Filter out disabled accounts and non-members after retrieval
            var enabledMembers = deltaRequest.Value
                .Where(u => u.AccountEnabled == true && u.UserType == "Member")
                .ToList();
            usersToProcess.AddRange(enabledMembers);
        }

        // Handle pagination
        while (!string.IsNullOrEmpty(deltaRequest?.OdataNextLink))
        {
            var nextPageRequest = new Microsoft.Graph.Users.Delta.DeltaRequestBuilder(
                deltaRequest.OdataNextLink,
                _graphClient.RequestAdapter);

            deltaRequest = await nextPageRequest.GetAsync();
            if (deltaRequest?.Value != null)
            {
                // Filter out disabled accounts and non-members after retrieval
                var enabledMembers = deltaRequest.Value
                    .Where(u => u.AccountEnabled == true && u.UserType == "Member")
                    .ToList();
                usersToProcess.AddRange(enabledMembers);
            }
        }

        return new DeltaQueryResult
        {
            Users = usersToProcess,
            DeltaLink = deltaRequest?.OdataDeltaLink
        };
    }

    /// <summary>
    /// Fetch only changed users using stored delta link.
    /// </summary>
    public async Task<DeltaQueryResult> FetchDeltaChangesAsync(string deltaLink)
    {
        var usersToProcess = new List<User>();

        // Use the delta link to get only changes
        var request = new Microsoft.Graph.Users.Delta.DeltaRequestBuilder(
            deltaLink,
            _graphClient.RequestAdapter);

        var deltaResponse = await request.GetAsync();

        // Collect first page of changes
        if (deltaResponse?.Value != null)
        {
            usersToProcess.AddRange(deltaResponse.Value);
        }

        // Handle pagination for delta changes
        while (!string.IsNullOrEmpty(deltaResponse?.OdataNextLink))
        {
            var nextPageRequest = new Microsoft.Graph.Users.Delta.DeltaRequestBuilder(
                deltaResponse.OdataNextLink,
                _graphClient.RequestAdapter);

            deltaResponse = await nextPageRequest.GetAsync();
            if (deltaResponse?.Value != null)
            {
                usersToProcess.AddRange(deltaResponse.Value);
            }
        }

        return new DeltaQueryResult
        {
            Users = usersToProcess,
            DeltaLink = deltaResponse?.OdataDeltaLink
        };
    }
}

