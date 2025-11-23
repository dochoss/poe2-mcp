namespace Poe2Mcp.Core.Services;

/// <summary>
/// Multi-tier cache service
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache (checks all tiers)
    /// </summary>
    /// <typeparam name="T">Type of value to retrieve</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value or null if not found/expired</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set value in cache (all tiers)
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="ttl">Time to live</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Delete key from all cache tiers
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all caches
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove expired entries from all caches
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of cache statistics</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
