using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Models.Profile;
using Poe2Mcp.Core.Models.League;
using Poe2Mcp.Core.Models.CurrencyExchange;
using Poe2Mcp.Core.Models.ItemFilter;

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
    /// Get account profile information
    /// Official API: GET /profile
    /// Required scope: account:profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile data or null if not available</returns>
    Task<ProfileData?> GetProfileAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List available leagues
    /// Official API: GET /league
    /// Required scope: service:leagues
    /// </summary>
    /// <param name="realm">Realm (pc, xbox, sony, poe2). Default is pc</param>
    /// <param name="type">Type filter: main (default), event, or season</param>
    /// <param name="limit">Maximum results (default 50, max 50)</param>
    /// <param name="offset">Offset for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leagues</returns>
    Task<IReadOnlyList<League>> ListLeaguesAsync(
        string? realm = null,
        string type = "main",
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get details for a specific league
    /// Official API: GET /league/{league}
    /// Required scope: service:leagues
    /// </summary>
    /// <param name="leagueName">Name of the league</param>
    /// <param name="realm">Realm (pc, xbox, sony, poe2). Default is pc</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>League data or null if not found</returns>
    Task<League?> GetLeagueAsync(
        string leagueName,
        string? realm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch ladder entries from the official API
    /// </summary>
    /// <param name="league">League name (already normalized for API)</param>
    /// <param name="limit">Number of entries per page (max 200)</param>
    /// <param name="offset">Offset for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ladder entries</returns>
    Task<IReadOnlyList<LadderEntry>> GetLadderEntriesAsync(
        string league,
        int limit = 200,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get currency exchange market data
    /// Official API: GET /currency-exchange[/realm][/id]
    /// Required scope: service:cxapi
    /// 
    /// Returns aggregate Currency Exchange trade history, grouped into hourly digests.
    /// Responses are purely historical - cannot get data from current hour.
    /// </summary>
    /// <param name="realm">Realm (pc, xbox, sony, poe2). Default is pc</param>
    /// <param name="timestamp">Unix timestamp for historical data. If null, returns first hour of history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency exchange market data</returns>
    Task<CurrencyExchangeResponse?> GetCurrencyExchangeMarketsAsync(
        string? realm = null,
        uint? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all item filters on the account
    /// Official API: GET /item-filter
    /// Required scope: account:item_filter
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of item filters</returns>
    Task<IReadOnlyList<ItemFilter>> ListItemFiltersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific item filter by ID
    /// Official API: GET /item-filter/{id}
    /// Required scope: account:item_filter
    /// </summary>
    /// <param name="filterId">Item filter ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Item filter or null if not found</returns>
    Task<ItemFilter?> GetItemFilterAsync(
        string filterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new item filter
    /// Official API: POST /item-filter
    /// Required scope: account:item_filter
    /// </summary>
    /// <param name="request">Filter creation request</param>
    /// <param name="validate">Whether to validate against current game version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created item filter or null if failed</returns>
    Task<ItemFilter?> CreateItemFilterAsync(
        CreateItemFilterRequest request,
        bool validate = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing item filter
    /// Official API: POST /item-filter/{id}
    /// Required scope: account:item_filter
    /// 
    /// This endpoint allows partial updates. Properties not supplied will use current values.
    /// Note: A public filter cannot be made private again.
    /// </summary>
    /// <param name="filterId">Item filter ID</param>
    /// <param name="request">Filter update request (partial)</param>
    /// <param name="validate">Whether to validate against current game version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated item filter or null if failed</returns>
    Task<ItemFilter?> UpdateItemFilterAsync(
        string filterId,
        UpdateItemFilterRequest request,
        bool validate = false,
        CancellationToken cancellationToken = default);
}
