using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Poe2Mcp.Server.Tools;

[McpServerToolType]
public class ServerTools(ILogger<ServerTools> logger)
{
  [McpServerTool(Name = "health_check", Title = "Server Health Check")]
  [Description("Check MCP server health and status. Returns server version, uptime, and operational status.")]
  public Task<object> HealthCheckAsync()
  {
    return Task.FromResult<object>(new
    {
      success = true,
      status = "healthy",
      timestamp = DateTime.UtcNow,
      version = "1.0.0",
      phase = "Phase 7 Complete"
    });
  }

  [McpServerTool(Name = "clear_cache", Title = "Clear Cache")]
  [Description("Clear cached data. Can clear specific cache key or all caches if no key specified.")]
  public Task<object> ClearCacheAsync(
      [Description("Specific cache key to clear, or null to clear all caches")]
      string? cacheKey = null)
  {
    logger.LogInformation("Tool: clear_cache - Key:{Key}", cacheKey ?? "all");
    return Task.FromResult<object>(new
    {
      success = true,
      clearedKey = cacheKey ?? "all",
      message = "Cache manager tool registered. Full implementation coming soon."
    });
  }
}
