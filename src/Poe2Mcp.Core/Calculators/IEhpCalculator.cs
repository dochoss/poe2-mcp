using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Interface for Effective Health Pool (EHP) calculator.
/// </summary>
public interface IEhpCalculator
{
    /// <summary>
    /// Calculate EHP against all damage types.
    /// </summary>
    /// <param name="stats">Defensive stats of the character.</param>
    /// <param name="threatProfile">Optional threat profile defining expected hit sizes.</param>
    /// <returns>Dictionary mapping damage type to EHP value.</returns>
    Dictionary<string, double> CalculateEhp(
        DefensiveStats stats,
        ThreatProfile? threatProfile = null);

    /// <summary>
    /// Calculate EHP for all damage types and return detailed results.
    /// </summary>
    /// <param name="stats">Defensive stats of the character.</param>
    /// <param name="expectedHitSize">Expected hit size for calculations.</param>
    /// <returns>List of detailed EHP results for each damage type.</returns>
    IReadOnlyList<EhpResult> CalculateEhpDetailed(
        DefensiveStats stats, 
        int expectedHitSize = 1000);
}
