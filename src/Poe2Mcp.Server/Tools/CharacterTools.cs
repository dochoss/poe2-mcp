using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Poe2Mcp.Server.Tools;

[McpServerToolType]
public class CharacterTools(ILogger<CharacterTools> logger)
{
  [McpServerTool(Name = "analyze_character", Title = "Analyze PoE2 Character")]
  [Description("Analyze a Path of Exile 2 character's build, stats, gear, and passive tree. Returns comprehensive build information.")]
  public Task<object> AnalyzeCharacterAsync(
        [Description("Account name")]
        string account,
        [Description("Character name")]
        string character,
        [Description("League name (default: 'Standard')")]
        string league = "Standard")
  {
    logger.LogInformation("Tool: analyze_character - {Account}/{Character} in {League}", account, character, league);
    return Task.FromResult<object>(new
    {
      success = true,
      account,
      character,
      league,
      message = "Character analysis tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "compare_to_top_players", Title = "Compare to Top Players")]
  [Description("Compare a character to top ladder players. Identifies gaps in build optimization and performance.")]
  public Task<object> CompareToTopPlayersAsync(
      [Description("Account name")]
      string account,
      [Description("Character name")]
      string character,
      [Description("League name (default: 'Standard')")]
      string league = "Standard",
      [Description("Number of top players to compare against (default: 10)")]
      int topN = 10)
  {
    logger.LogInformation("Tool: compare_to_top_players - {Account}/{Character}", account, character);
    return Task.FromResult<object>(new
    {
      success = true,
      account,
      character,
      league,
      topN,
      message = "Ladder comparison tool registered. Full implementation coming soon."
    });
  }
}
