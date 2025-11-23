using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Service for optimizing gear upgrades
/// </summary>
public interface IGearOptimizer
{
    /// <summary>
    /// Generate gear optimization recommendations
    /// </summary>
    /// <param name="characterData">Character data with current gear</param>
    /// <param name="budget">Budget tier (low/medium/high/unlimited)</param>
    /// <param name="goal">Optimization goal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Gear recommendations with priorities and suggestions</returns>
    Task<GearOptimizationResult> OptimizeAsync(
        CharacterData characterData,
        BudgetTier budget = BudgetTier.Medium,
        OptimizationGoal goal = OptimizationGoal.Balanced,
        CancellationToken cancellationToken = default);
}
