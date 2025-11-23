using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Service for calculating optimal spell + support gem combinations
/// </summary>
public interface IGemSynergyCalculator
{
    /// <summary>
    /// Find the best support gem combinations for a spell
    /// </summary>
    /// <param name="spellName">Name or ID of the spell gem</param>
    /// <param name="characterMods">Character modifiers (increased damage, cast speed, etc.)</param>
    /// <param name="maxSpirit">Maximum spirit available</param>
    /// <param name="numSupports">Number of support gems to use (1-5)</param>
    /// <param name="goal">Optimization goal</param>
    /// <param name="topN">Number of top combinations to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top N synergy results, sorted by score</returns>
    Task<IReadOnlyList<SynergyResult>> FindBestCombinationsAsync(
        string spellName,
        CharacterModifiers? characterMods = null,
        int maxSpirit = 100,
        int numSupports = 5,
        OptimizationGoal goal = OptimizationGoal.Dps,
        int topN = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Format a synergy result as human-readable text
    /// </summary>
    /// <param name="result">Synergy result to format</param>
    /// <param name="detailed">Include detailed calculation breakdown</param>
    /// <returns>Formatted string</returns>
    string FormatResult(SynergyResult result, bool detailed = false);
}
