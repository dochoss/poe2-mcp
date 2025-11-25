using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Poe2Mcp.Server.Tools;

[McpServerToolType]
public class OptimizerTools(ILogger<OptimizerTools> logger)
{
  [McpServerTool(Name = "optimize_gear", Title = "Optimize Gear")]
  [Description("Optimize gear selections for a build. Suggests best-in-slot items based on goal and budget.")]
  public Task<object> OptimizeGearAsync(
        [Description("Optimization goal: 'Balanced', 'DPS', 'Defense', 'Speed' (default: 'Balanced')")]
        string goal = "Balanced",
        [Description("Budget tier: 'Low', 'Medium', 'High', 'Unlimited' (default: 'Medium')")]
        string budget = "Medium")
  {
    logger.LogInformation("Tool: optimize_gear - Goal:{Goal} Budget:{Budget}", goal, budget);
    return Task.FromResult<object>(new
    {
      success = true,
      goal,
      budget,
      message = "Gear optimizer tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "optimize_passive_tree", Title = "Optimize Passive Tree")]
  [Description("Optimize passive skill tree allocation. Suggests efficient pathing and node selection based on available points.")]
  public Task<object> OptimizePassiveTreeAsync(
      [Description("Number of passive points available to allocate (default: 0)")]
      int availablePoints = 0,
      [Description("Optimization goal: 'Balanced', 'DPS', 'Defense', 'Utility' (default: 'Balanced')")]
      string goal = "Balanced")
  {
    logger.LogInformation("Tool: optimize_passive_tree - Points:{Points} Goal:{Goal}", availablePoints, goal);
    return Task.FromResult<object>(new
    {
      success = true,
      availablePoints,
      goal,
      message = "Passive tree optimizer tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "optimize_skills", Title = "Optimize Skills")]
  [Description("Optimize skill gem setup and support gems. Maximizes effectiveness within spirit budget.")]
  public Task<object> OptimizeSkillsAsync(
      [Description("Primary active skill gem name")]
      string primarySkill,
      [Description("Available spirit points for skill reservation (default: 0)")]
      int availableSpirit = 0)
  {
    logger.LogInformation("Tool: optimize_skills - Skill:{Skill} Spirit:{Spirit}", primarySkill, availableSpirit);
    return Task.FromResult<object>(new
    {
      success = true,
      primarySkill,
      availableSpirit,
      message = "Skill optimizer tool registered. Full implementation coming soon."
    });
  }

  [McpServerTool(Name = "find_best_supports", Title = "Find Best Support Gems")]
  [Description("Find optimal support gems for a skill. Ranks supports by damage and utility contribution.")]
  public Task<object> FindBestSupportsAsync(
      [Description("Active skill gem name to find supports for")]
      string skillName,
      [Description("Maximum number of support gems to suggest (default: 5)")]
      int maxSupportGems = 5)
  {
    logger.LogInformation("Tool: find_best_supports - Skill:{Skill} Max:{Max}", skillName, maxSupportGems);
    return Task.FromResult<object>(new
    {
      success = true,
      skillName,
      maxSupportGems,
      message = "Support gem optimizer tool registered. Full implementation coming soon."
    });
  }
}
