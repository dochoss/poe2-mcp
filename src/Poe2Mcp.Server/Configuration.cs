namespace Poe2Mcp.Server;

/// <summary>
/// Configuration options for the MCP server
/// </summary>
public class McpServerOptions
{
    public string Name { get; set; } = "poe2-build-optimizer";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Configuration options for database
/// </summary>
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "Data Source=poe2_optimizer.db";
    public bool EnableSensitiveDataLogging { get; set; } = false;
}

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
/// Feature flags
/// </summary>
public class FeaturesOptions
{
    public bool EnableAiInsights { get; set; } = true;
    public bool EnableTradeIntegration { get; set; } = true;
    public bool EnablePobExport { get; set; } = true;
}
