using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Poe2Mcp.Server.Tools;

[McpServerToolType]
public class AITools(ILogger<AITools> logger)
{
  [McpServerTool(Name = "natural_language_query", Title = "Natural Language Query")]
  [Description("Process natural language queries about builds, mechanics, or game systems. Returns AI-generated responses.")]
  public Task<object> NaturalLanguageQueryAsync(
      [Description("Natural language question or query about PoE2")]
      string query)
  {
    logger.LogInformation("Tool: natural_language_query - Query:{Query}", query == null ? "null" : query.Length > 50 ? query.Substring(0, 50) : query);
    return Task.FromResult<object>(new
    {
      success = true,
      query,
      message = "AI query handler tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "explain_mechanic", Title = "Explain Game Mechanic")]
  [Description("Explain Path of Exile 2 game mechanics, systems, or interactions. Provides detailed explanations.")]
  public Task<object> ExplainMechanicAsync(
      [Description("Game mechanic name or system to explain")]
      string mechanicName)
  {
    logger.LogInformation("Tool: explain_mechanic - Mechanic:{Mechanic}", mechanicName);
    return Task.FromResult<object>(new
    {
      success = true,
      mechanicName,
      message = "Mechanics explainer tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "compare_items", Title = "Compare Items")]
  [Description("Compare two items side-by-side. Highlights stat differences and recommends better choice for build.")]
  public Task<object> CompareItemsAsync(
      [Description("First item name")]
      string item1Name,
      [Description("Second item name")]
      string item2Name)
  {
    logger.LogInformation("Tool: compare_items - {Item1} vs {Item2}", item1Name, item2Name);
    return Task.FromResult<object>(new
    {
      success = true,
      item1 = item1Name,
      item2 = item2Name,
      message = "Item comparator tool registered. Full implementation coming soon."
    });
  }
}
