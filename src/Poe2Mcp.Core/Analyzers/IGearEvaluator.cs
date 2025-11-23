using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Interface for gear evaluation and upgrade analysis.
/// Quantifies the exact impact of gear upgrades on character performance.
/// </summary>
public interface IGearEvaluator
{
    /// <summary>
    /// Evaluate a gear upgrade by calculating exact stat changes.
    /// </summary>
    /// <param name="currentGear">Currently equipped gear.</param>
    /// <param name="upgradeGear">Potential upgrade gear.</param>
    /// <param name="baseCharacterStats">Base character stats (without this gear piece).</param>
    /// <param name="threatProfile">Optional threat profile for EHP calculations.</param>
    /// <param name="priceChaos">Optional price of upgrade in chaos orbs.</param>
    /// <returns>Detailed upgrade value analysis.</returns>
    UpgradeValue EvaluateUpgrade(
        GearStats currentGear,
        GearStats upgradeGear,
        CharacterStats baseCharacterStats,
        ThreatProfile? threatProfile = null,
        double? priceChaos = null);

    /// <summary>
    /// Evaluate multiple upgrade options and rank them.
    /// </summary>
    /// <param name="currentGear">Currently equipped gear.</param>
    /// <param name="potentialUpgrades">List of potential upgrades with optional prices.</param>
    /// <param name="baseCharacterStats">Base character stats.</param>
    /// <param name="topN">Number of top upgrades to return.</param>
    /// <returns>List of upgrades sorted by priority score.</returns>
    IReadOnlyList<(GearStats Gear, UpgradeValue Value)> EvaluateMultipleUpgrades(
        GearStats currentGear,
        IEnumerable<(GearStats Gear, double? Price)> potentialUpgrades,
        CharacterStats baseCharacterStats,
        int topN = 5);

    /// <summary>
    /// Direct comparison of two items.
    /// </summary>
    /// <param name="itemA">First item to compare.</param>
    /// <param name="itemB">Second item to compare.</param>
    /// <param name="baseCharacterStats">Base character stats.</param>
    /// <returns>Comparison results showing which is better.</returns>
    ItemComparison CompareItems(
        GearStats itemA,
        GearStats itemB,
        CharacterStats baseCharacterStats);
}
