using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Interface for build scoring and analysis.
/// Evaluates overall build quality and provides tier rankings.
/// </summary>
public interface IBuildScorer
{
    /// <summary>
    /// Analyze a build and calculate comprehensive scores.
    /// </summary>
    /// <param name="characterData">Character data to analyze.</param>
    /// <returns>Build score with tier, strengths, and weaknesses.</returns>
    BuildScore AnalyzeBuild(CharacterData characterData);

    /// <summary>
    /// Score the quality of equipped gear.
    /// </summary>
    /// <param name="characterData">Character data.</param>
    /// <returns>Gear score (0.0 to 1.0).</returns>
    double ScoreGear(CharacterData characterData);

    /// <summary>
    /// Score the efficiency of the passive tree.
    /// </summary>
    /// <param name="passiveCount">Number of passives allocated.</param>
    /// <param name="level">Character level.</param>
    /// <returns>Passive tree score (0.0 to 1.0).</returns>
    double ScorePassiveTree(int passiveCount, int level);

    /// <summary>
    /// Score the skill setup quality.
    /// </summary>
    /// <param name="characterData">Character data.</param>
    /// <returns>Skill score (0.0 to 1.0).</returns>
    double ScoreSkills(CharacterData characterData);

    /// <summary>
    /// Calculate defense rating based on defensive stats.
    /// </summary>
    /// <param name="stats">Defensive statistics.</param>
    /// <returns>Defense rating (0.0 to 1.0).</returns>
    double CalculateDefenseRating(DefensiveStats stats);
}
