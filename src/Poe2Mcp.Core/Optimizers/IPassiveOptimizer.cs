using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Service for optimizing passive tree allocation
/// </summary>
public interface IPassiveOptimizer
{
    /// <summary>
    /// Generate passive tree recommendations
    /// </summary>
    /// <param name="characterData">Character data with current passive allocations</param>
    /// <param name="availablePoints">Number of passive points available</param>
    /// <param name="allowRespec">Whether to suggest respec options</param>
    /// <param name="goal">Optimization goal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Passive allocation and respec recommendations</returns>
    Task<PassiveOptimizationResult> OptimizeAsync(
        CharacterData characterData,
        int availablePoints = 0,
        bool allowRespec = false,
        OptimizationGoal goal = OptimizationGoal.Balanced,
        CancellationToken cancellationToken = default);
}
