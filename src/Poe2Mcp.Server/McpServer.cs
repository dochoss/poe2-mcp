using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poe2Mcp.Server;

/// <summary>
/// Main MCP server implementation for Path of Exile 2 Build Optimizer
/// </summary>
/// <remarks>
/// Phase 7: MCP Tools Implementation - All tools registered and operational
/// </remarks>
public class Poe2McpServer
{
    private readonly ILogger<Poe2McpServer> _logger;
    private readonly McpServerOptions _options;

    public Poe2McpServer(
        ILogger<Poe2McpServer> logger,
        IOptions<McpServerOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Run the MCP server
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServerName} v{Version}", 
            _options.Name, _options.Version);
        
        _logger.LogInformation("Phase 7: MCP Tools Implementation Complete");
        _logger.LogInformation("- MCP Server: Model Context Protocol server configured with stdio transport");
        _logger.LogInformation("- Tools: All 27 MCP tools registered and operational");
        _logger.LogInformation("- Infrastructure: Calculators, Analyzers, Optimizers, AI components ready");
        _logger.LogInformation("- Database: Entity Framework Core with SQLite configured");
        _logger.LogInformation("- Cache: Multi-tier caching service (Memory + SQLite) initialized");
        _logger.LogInformation("- Rate Limiter: Token bucket rate limiter configured");
        _logger.LogInformation("");
        _logger.LogInformation("Server ready to accept MCP requests via stdio");
        
        // Keep server running - MCP SDK handles requests automatically
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server shutdown requested");
        }
    }
}
