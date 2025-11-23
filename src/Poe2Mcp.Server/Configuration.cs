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
/// Feature flags
/// </summary>
public class FeaturesOptions
{
    public bool EnableAiInsights { get; set; } = true;
    public bool EnableTradeIntegration { get; set; } = true;
    public bool EnablePobExport { get; set; } = true;
}
