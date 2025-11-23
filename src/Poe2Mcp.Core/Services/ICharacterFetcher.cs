using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Fetches character data from multiple sources with intelligent fallback
/// </summary>
public interface ICharacterFetcher
{
    /// <summary>
    /// Fetch character data using all available sources with intelligent fallback
    /// Priority: 1) poe.ninja API, 2) Official ladder API
    /// </summary>
    /// <param name="accountName">PoE account name</param>
    /// <param name="characterName">Character name</param>
    /// <param name="league">League name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Character data or null if not found</returns>
    Task<CharacterData?> GetCharacterAsync(
        string accountName,
        string characterName,
        string league = "Standard",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top ladder characters
    /// </summary>
    /// <param name="league">League name</param>
    /// <param name="limit">Number of characters to return</param>
    /// <param name="minLevel">Minimum level filter</param>
    /// <param name="classFilter">Filter by character class</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of character data</returns>
    Task<IReadOnlyList<LadderEntry>> GetTopLadderCharactersAsync(
        string league = "Standard",
        int limit = 100,
        int minLevel = 1,
        string? classFilter = null,
        CancellationToken cancellationToken = default);
}
