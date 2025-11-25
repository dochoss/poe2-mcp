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

/// <summary>
/// Configuration options for official PoE API
/// </summary>
public class PoeApiOptions
{
    public string BaseUrl { get; set; } = "https://www.pathofexile.com/api";
    public int CacheTtlSeconds { get; set; } = 300; // 5 minutes
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Realm to use for API calls. Can be: pc (default), xbox, sony, or poe2.
    /// If omitted or "pc", PoE1 PC realm is assumed.
    /// </summary>
    public string? Realm { get; set; } = "poe2";
}

/// <summary>
/// Configuration options for poe.ninja API
/// </summary>
public class PoeNinjaOptions
{
    public string BaseUrl { get; set; } = "https://poe.ninja";
    public int CacheTtlSeconds { get; set; } = 3600; // 1 hour
    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration options for trade API
/// </summary>
public class TradeApiOptions
{
    public string BaseUrl { get; set; } = "https://www.pathofexile.com";
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string? PoeSessionId { get; set; }
    public int RequestDelayMilliseconds { get; set; } = 500;
}

/// <summary>
/// Configuration options for character fetcher
/// </summary>
public class CharacterFetcherOptions
{
    public int MaxLadderSearchDepth { get; set; } = 1000;
    public int LadderPageSize { get; set; } = 200;
}

/// <summary>
/// Configuration options for AI integration
/// </summary>
public class AIOptions
{
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string Model { get; set; } = "gpt-oss-120b";
    public int? MaxTokens { get => field is null or 0 ? int.MaxValue : field.Value; set; }
    public double Temperature { get; set; } = 0.7;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
