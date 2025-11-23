using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for poe.ninja API
/// </summary>
public interface IPoeNinjaApiClient
{
    /// <summary>
    /// Fetch character data from poe.ninja
    /// </summary>
    /// <param name="account">Account name</param>
    /// <param name="character">Character name</param>
    /// <param name="league">League name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Character data or null if not found</returns>
    Task<CharacterData?> GetCharacterAsync(
        string account,
        string character,
        string league = "Abyss",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top builds from poe.ninja ladder
    /// </summary>
    /// <param name="league">League name</param>
    /// <param name="className">Filter by character class</param>
    /// <param name="skill">Filter by main skill</param>
    /// <param name="limit">Maximum number of builds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of build data</returns>
    Task<IReadOnlyList<CharacterData>> GetTopBuildsAsync(
        string league = "Standard",
        string? className = null,
        string? skill = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get item prices from poe.ninja economy API
    /// </summary>
    /// <param name="league">League name</param>
    /// <param name="itemType">Type of items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of items with prices</returns>
    Task<IReadOnlyList<NinjaItemPrice>> GetItemPricesAsync(
        string league = "Standard",
        string itemType = "UniqueWeapon",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Path of Building import code for a character
    /// </summary>
    /// <param name="account">Account name</param>
    /// <param name="character">Character name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded PoB build code or null if not found</returns>
    Task<string?> GetPobImportAsync(
        string account,
        string character,
        CancellationToken cancellationToken = default);
}
