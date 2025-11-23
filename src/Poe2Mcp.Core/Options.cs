namespace Poe2Mcp.Core;

/// <summary>
/// Configuration options for caching
/// </summary>
public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 60;
    public int CharacterCacheMinutes { get; set; } = 5;
    public int ApiCacheMinutes { get; set; } = 30;
}

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    public int OfficialApiRequestsPerMinute { get; set; } = 10;
    public int Poe2DbRequestsPerMinute { get; set; } = 30;
}

/// <summary>
/// Configuration options for database
/// </summary>
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "Data Source=data/poe2_optimizer.db";
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
