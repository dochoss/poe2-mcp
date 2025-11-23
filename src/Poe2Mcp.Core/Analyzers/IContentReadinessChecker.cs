using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Interface for content readiness checking.
/// Determines if a character has sufficient defenses for specific endgame content.
/// </summary>
public interface IContentReadinessChecker
{
    /// <summary>
    /// Check if character is ready for specific content.
    /// </summary>
    /// <param name="stats">Character's defensive statistics.</param>
    /// <param name="contentDifficulty">The content difficulty to check.</param>
    /// <param name="dps">Optional DPS value for damage checks.</param>
    /// <returns>Readiness report with assessment and recommendations.</returns>
    ReadinessReport CheckReadiness(
        DefensiveStats stats,
        ContentDifficulty contentDifficulty,
        double dps = 0);

    /// <summary>
    /// Get defense requirements for specific content.
    /// </summary>
    /// <param name="contentDifficulty">The content difficulty.</param>
    /// <returns>Defense requirements for that content.</returns>
    DefenseRequirement GetRequirements(ContentDifficulty contentDifficulty);

    /// <summary>
    /// Get all available content tiers and their requirements.
    /// </summary>
    /// <returns>Dictionary of content difficulties and their requirements.</returns>
    IReadOnlyDictionary<ContentDifficulty, DefenseRequirement> GetAllRequirements();
}
