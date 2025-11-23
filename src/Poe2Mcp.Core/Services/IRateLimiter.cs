namespace Poe2Mcp.Core.Services;

/// <summary>
/// Rate limiting service for API calls
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Wait for rate limit slot to become available
    /// </summary>
    /// <param name="endpoint">API endpoint identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WaitAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if request is allowed without waiting
    /// </summary>
    /// <param name="endpoint">API endpoint identifier</param>
    /// <returns>True if request is allowed, false if rate limited</returns>
    bool IsAllowed(string endpoint);

    /// <summary>
    /// Get remaining requests for endpoint
    /// </summary>
    /// <param name="endpoint">API endpoint identifier</param>
    /// <returns>Number of remaining requests in current window</returns>
    int GetRemainingRequests(string endpoint);
}
