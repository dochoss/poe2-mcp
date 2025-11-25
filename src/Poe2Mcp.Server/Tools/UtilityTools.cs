using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Poe2Mcp.Server.Tools;

[McpServerToolType]
public class UtilityTools(ILogger<UtilityTools> logger)
{
  [McpServerTool(Name = "search_trade_items", Title = "Search Trade Items")]
  [Description("Search Path of Exile 2 trade market for items that improve your character. Finds gear to address deficiencies like missing resistances, low life/ES, or needed stats.")]
  public Task<object> SearchTradeItemsAsync(
      [Description("Item name or search query")]
      string itemName,
      [Description("League name (e.g., 'Standard', 'Abyss')")]
      string league = "Standard")
  {
    logger.LogInformation("Tool: search_trade_items - Item:{Item} League:{League}", itemName, league);
    return Task.FromResult<object>(new
    {
      success = true,
      itemName,
      league,
      message = "Trade search tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "import_pob", Title = "Import from Path of Building")]
  [Description("Import a Path of Building build code for analysis. Accepts base64-encoded PoB codes.")]
  public Task<object> ImportPobAsync(
      [Description("Path of Building build code (base64 encoded)")]
      string pobCode)
  {
    logger.LogInformation("Tool: import_pob - Code length:{Length}", pobCode?.Length ?? 0);
    return Task.FromResult<object>(new
    {
      success = true,
      codeLength = pobCode?.Length ?? 0,
      message = "PoB importer tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "export_pob", Title = "Export to Path of Building")]
  [Description("Export character to Path of Building format. Generates a PoB-compatible build code.")]
  public Task<object> ExportPobAsync(
      [Description("Account name")]
      string account,
      [Description("Character name")]
      string character,
      [Description("League name (default: 'Standard')")]
      string league = "Standard")
  {
    logger.LogInformation("Tool: export_pob - {Account}/{Character}", account, character);
    return Task.FromResult<object>(new
    {
      success = true,
      account,
      character,
      league,
      message = "PoB exporter tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "get_pob_code", Title = "Get PoB Code")]
  [Description("Get Path of Building import code for a character from poe.ninja. Fetches a ready-to-use PoB code that can be imported into Path of Building application.")]
  public Task<object> GetPobCodeAsync(
      [Description("Path of Exile account name")]
      string account,
      [Description("Character name")]
      string character,
      [Description("League name (default: 'Standard')")]
      string league = "Standard")
  {
    logger.LogInformation("Tool: get_pob_code - {Account}/{Character}", account, character);
    return Task.FromResult<object>(new
    {
      success = true,
      account,
      character,
      league,
      message = "PoB code generator tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "setup_trade_auth", Title = "Setup Trade Authentication")]
  [Description("Set up Path of Exile trade API authentication by opening a browser and automatically extracting your session cookie. Required before using search_trade_items.")]
  public Task<object> SetupTradeAuthAsync(
      [Description("Trade session ID (POESESSID cookie value)")]
      string sessionId)
  {
    logger.LogWarning("Tool: setup_trade_auth - Called (session ID redacted for security)");
    return Task.FromResult<object>(new
    {
      success = true,
      message = "Trade auth setup tool registered. SECURITY NOTE: Never hardcode or commit session IDs."
    });
  }

  [McpServerTool(Name = "compare_builds", Title = "Compare Builds")]
  [Description("Compare multiple builds and highlight differences. Analyzes DPS, defense, cost, and other metrics.")]
  public Task<object> CompareBuildsAsync(
      [Description("First account name")]
      string account1,
      [Description("First character name")]
      string character1,
      [Description("Second account name")]
      string account2,
      [Description("Second character name")]
      string character2,
      [Description("League name (default: 'Standard')")]
      string league = "Standard")
  {
    logger.LogInformation("Tool: compare_builds - {A1}/{C1} vs {A2}/{C2}", account1, character1, account2, character2);
    return Task.FromResult<object>(new
    {
      success = true,
      build1 = new { account = account1, character = character1 },
      build2 = new { account = account2, character = character2 },
      league,
      message = "Build comparator tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "search_items", Title = "Search Items")]
  [Description("Search for items in the game database. Supports filters for rarity, item class, and other properties.")]
  public Task<object> SearchItemsAsync(
      [Description("Item name or type to search for")]
      string query,
      [Description("Maximum number of results to return (default: 10)")]
      int limit = 10)
  {
    logger.LogInformation("Tool: search_items - Query:{Query} Limit:{Limit}", query, limit);
    return Task.FromResult<object>(new
    {
      success = true,
      query,
      limit,
      message = "Item search tool registered. Full implementation coming soon."
    });
  }
}
