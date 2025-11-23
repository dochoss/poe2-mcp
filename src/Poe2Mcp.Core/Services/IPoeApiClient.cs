using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for the official Path of Exile API
/// </summary>
public interface IPoeApiClient
{
    /// <summary>
    /// Fetch character data from the official API
    /// </summary>
    /// <param name="accountName">Path of Exile account name</param>
    /// <param name="characterName">Character name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Character data or null if not found</returns>
    Task<CharacterData?> GetCharacterAsync(
        string accountName, 
        string characterName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch all characters for an account
    /// </summary>
    /// <param name="accountName">Path of Exile account name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of character data</returns>
    Task<IReadOnlyList<CharacterData>> GetAccountCharactersAsync(
        string accountName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the passive skill tree data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Passive tree data</returns>
    Task<PassiveTreeData> GetPassiveTreeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch item data from the API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Item data dictionary</returns>
    Task<Dictionary<string, object>> GetItemsDataAsync(
        CancellationToken cancellationToken = default);
}
