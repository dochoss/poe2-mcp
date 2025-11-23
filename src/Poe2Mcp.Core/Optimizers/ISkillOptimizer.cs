using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Service for optimizing skill gem setups
/// </summary>
public interface ISkillOptimizer
{
    /// <summary>
    /// Generate skill setup recommendations
    /// </summary>
    /// <param name="characterData">Character data with current skills</param>
    /// <param name="goal">Optimization goal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Skill setup recommendations</returns>
    Task<SkillOptimizationResult> OptimizeAsync(
        CharacterData characterData,
        OptimizationGoal goal = OptimizationGoal.Balanced,
        CancellationToken cancellationToken = default);
}
