using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Models.Profile;
using Poe2Mcp.Core.Models.League;
using Poe2Mcp.Core.Models.CurrencyExchange;
using Poe2Mcp.Core.Models.ItemFilter;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for the official Path of Exile API
/// Implements OAuth 2.0, rate limiting, and caching
/// 
/// Official API Reference: https://www.pathofexile.com/developer/docs/reference
/// 
/// Supported Endpoints:
/// - GET /profile - Account profile information
/// - GET /character[/realm] - List account characters
/// - GET /character[/realm]/name - Get specific character
/// - GET /league - List leagues
/// - GET /league/league - Get specific league
/// - GET /ladders/league - Get ladder entries
/// - GET /currency-exchange[/realm][/id] - Currency exchange markets
/// - GET /item-filter - List item filters
/// - GET /item-filter/id - Get specific item filter
/// - POST /item-filter - Create item filter
/// - POST /item-filter/id - Update item filter
/// 
/// Note: Character and passive tree data are from the official API.
/// For static game data (items, skills, etc.), use the Data Exports:
/// https://www.pathofexile.com/developer/docs/data
/// </summary>
public class PoeApiClient : IPoeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PoeApiClient> _logger;
    private readonly ICacheService _cacheService;
    private readonly IRateLimiter _rateLimiter;
    private readonly PoeApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public PoeApiClient(
        HttpClient httpClient,
        ILogger<PoeApiClient> logger,
        ICacheService cacheService,
        IRateLimiter rateLimiter,
        IOptions<PoeApiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Configure user agent
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PoE2-Build-Optimizer/1.0");
        }
    }

    /// <inheritdoc/>
    public async Task<CharacterData?> GetCharacterAsync(
        string accountName,
        string characterName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        var cacheKey = $"character:{accountName}:{characterName}";

        // Check cache first
        var cachedData = await _cacheService.GetAsync<CharacterData>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for character {Character}", characterName);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            // According to official PoE API docs: GET /character[/<realm>]/<name>
            // The realm can be: xbox, sony, or poe2. If omitted, PoE1 PC realm is assumed.
            // The API returns: { "character": Character }
            // Note: The account is determined by OAuth authentication, not from the URL
            var url = !string.IsNullOrWhiteSpace(_options.Realm) && _options.Realm != "pc"
                ? $"/character/{_options.Realm}/{characterName}"
                : $"/character/{characterName}";
                
            _logger.LogInformation("Fetching character {Character} from account {Account}", characterName, accountName);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Character {Character} not found for account {Account}", characterName, accountName);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Character {Character} profile is private", characterName);
                return null;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when fetching character (OAuth token may be invalid)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            // The API returns: { "character": Character }
            var responseData = await response.Content.ReadFromJsonAsync<CharacterResponse>(_jsonOptions, cancellationToken);
            var characterData = responseData?.Character;

            // Cache the result
            if (characterData != null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    characterData,
                    TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                    cancellationToken);

                _logger.LogInformation("Successfully fetched character {Character}", characterName);
            }

            return characterData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching character {Character}: {Message}", characterName, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching character {Character}: {Message}", characterName, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CharacterData>> GetAccountCharactersAsync(
        string accountName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

        var cacheKey = $"account_characters:{accountName}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<CharacterData>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for account characters {Account}", accountName);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            // According to official PoE API docs: GET /character[/<realm>]
            // The realm can be: xbox, sony, or poe2. If omitted, PoE1 PC realm is assumed.
            // The API returns: { "characters": [array of Character] }
            // Note: The account is determined by OAuth authentication, not from the URL
            var url = "/character";
            
            // If realm preference is configured, append it
            if (!string.IsNullOrWhiteSpace(_options.Realm) && _options.Realm != "pc")
            {
                url = $"/character/{_options.Realm}";
            }
            
            _logger.LogInformation("Fetching characters for account {Account}", accountName);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when fetching characters (authentication may be required)");
                return [];
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when fetching characters (OAuth token may be invalid)");
                return [];
            }
            
            response.EnsureSuccessStatusCode();

            // The API returns: { "characters": [array of Character] }
            var responseData = await response.Content.ReadFromJsonAsync<CharacterListResponse>(_jsonOptions, cancellationToken);
            var characters = responseData?.Characters ?? [];

            // Cache the result
            await _cacheService.SetAsync(
                cacheKey,
                characters,
                TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} characters for account {Account}", characters.Count, accountName);

            return characters;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching account characters for {Account}: {Message}", accountName, ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account characters for {Account}: {Message}", accountName, ex.Message);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileData?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "account_profile";

        // Check cache
        var cachedData = await _cacheService.GetAsync<ProfileData>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for account profile");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            const string url = "/profile";
            _logger.LogInformation("Fetching account profile");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when fetching profile (OAuth token required)");
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when fetching profile (check OAuth scopes)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var profileData = await response.Content.ReadFromJsonAsync<ProfileData>(_jsonOptions, cancellationToken);

            // Cache the result
            if (profileData != null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    profileData,
                    TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                    cancellationToken);

                _logger.LogInformation("Successfully fetched account profile for {AccountName}", profileData.Name);
            }

            return profileData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching account profile: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account profile: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<League>> ListLeaguesAsync(
        string? realm = null,
        string type = "main",
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 50");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
        }

        var effectiveRealm = realm ?? _options.Realm ?? "pc";
        var cacheKey = $"leagues:{effectiveRealm}:{type}:{limit}:{offset}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<League>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for leagues list");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/league?realm={effectiveRealm}&type={type}&limit={limit}&offset={offset}";
            _logger.LogDebug("Fetching leagues: realm={Realm}, type={Type}, limit={Limit}, offset={Offset}",
                effectiveRealm, type, limit, offset);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<LeaguesResponse>(_jsonOptions, cancellationToken);
            var leagues = data?.Leagues ?? [];

            _logger.LogInformation("Retrieved {Count} leagues", leagues.Count);

            // Cache for 5 minutes (leagues don't change frequently)
            if (leagues.Any())
            {
                await _cacheService.SetAsync(cacheKey, leagues, TimeSpan.FromMinutes(5), cancellationToken);
            }

            return leagues;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching leagues: {Message}", ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching leagues: {Message}", ex.Message);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<League?> GetLeagueAsync(
        string leagueName,
        string? realm = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leagueName);

        var effectiveRealm = realm ?? _options.Realm ?? "pc";
        var cacheKey = $"league:{effectiveRealm}:{leagueName}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<League>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for league {League}", leagueName);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/league/{leagueName}?realm={effectiveRealm}";
            _logger.LogInformation("Fetching league {League} for realm {Realm}", leagueName, effectiveRealm);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("League {League} not found", leagueName);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<LeagueResponse>(_jsonOptions, cancellationToken);
            var league = data?.League;

            // Cache the result
            if (league != null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    league,
                    TimeSpan.FromMinutes(5),
                    cancellationToken);

                _logger.LogInformation("Successfully fetched league {League}", leagueName);
            }

            return league;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching league {League}: {Message}", leagueName, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching league {League}: {Message}", leagueName, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LadderEntry>> GetLadderEntriesAsync(
        string league,
        int limit = 200,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(league);

        if (limit < 1 || limit > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 200");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/ladders/{league}?limit={limit}&offset={offset}";
            _logger.LogDebug("Fetching ladder entries: {League}, limit={Limit}, offset={Offset}", league, limit, offset);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<LadderResponse>(_jsonOptions, cancellationToken);
            var entries = data?.Entries ?? [];

            _logger.LogDebug("Retrieved {Count} ladder entries", entries.Count);

            return entries;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching ladder for {League}: {Message}", league, ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ladder for {League}: {Message}", league, ex.Message);
            return [];
        }
    }

    // Internal models for API responses
    private class LadderResponse
    {
        public List<LadderEntry> Entries { get; set; } = [];
    }

    /// <inheritdoc/>
    public async Task<CurrencyExchangeResponse?> GetCurrencyExchangeMarketsAsync(
        string? realm = null,
        uint? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveRealm = realm ?? _options.Realm ?? "pc";
        var cacheKey = $"currency_exchange:{effectiveRealm}:{timestamp}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<CurrencyExchangeResponse>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for currency exchange markets");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/currency-exchange/{effectiveRealm}";
            if (timestamp.HasValue)
            {
                url += $"/{timestamp.Value}";
            }

            _logger.LogDebug("Fetching currency exchange markets: realm={Realm}, timestamp={Timestamp}",
                effectiveRealm, timestamp);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<CurrencyExchangeResponse>(_jsonOptions, cancellationToken);

            if (data != null)
            {
                _logger.LogInformation("Retrieved {Count} currency markets", data.Markets.Count);

                // Cache for 1 hour (historical data doesn't change)
                await _cacheService.SetAsync(cacheKey, data, TimeSpan.FromHours(1), cancellationToken);
            }

            return data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching currency exchange markets: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency exchange markets: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ItemFilter>> ListItemFiltersAsync(
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "item_filters_list";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<ItemFilter>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for item filters list");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            const string url = "/item-filter";
            _logger.LogInformation("Fetching item filters list");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when fetching item filters (OAuth token required)");
                return [];
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when fetching item filters (check OAuth scopes)");
                return [];
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<ItemFiltersResponse>(_jsonOptions, cancellationToken);
            var filters = data?.Filters ?? [];

            _logger.LogInformation("Retrieved {Count} item filters", filters.Count);

            // Cache for 5 minutes
            if (filters.Any())
            {
                await _cacheService.SetAsync(cacheKey, filters, TimeSpan.FromMinutes(5), cancellationToken);
            }

            return filters;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching item filters: {Message}", ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item filters: {Message}", ex.Message);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<ItemFilter?> GetItemFilterAsync(
        string filterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterId);

        var cacheKey = $"item_filter:{filterId}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<ItemFilter>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for item filter {FilterId}", filterId);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/item-filter/{filterId}";
            _logger.LogInformation("Fetching item filter {FilterId}", filterId);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item filter {FilterId} not found", filterId);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when fetching item filter (OAuth token required)");
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when fetching item filter (check OAuth scopes)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<ItemFilterResponse>(_jsonOptions, cancellationToken);
            var filter = data?.Filter;

            if (filter != null)
            {
                _logger.LogInformation("Successfully fetched item filter {FilterId}", filterId);

                // Cache for 5 minutes
                await _cacheService.SetAsync(cacheKey, filter, TimeSpan.FromMinutes(5), cancellationToken);
            }

            return filter;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching item filter {FilterId}: {Message}", filterId, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item filter {FilterId}: {Message}", filterId, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ItemFilter?> CreateItemFilterAsync(
        CreateItemFilterRequest request,
        bool validate = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FilterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Realm);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Filter);

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = "/item-filter";
            if (validate)
            {
                url += "?validate=true";
            }

            _logger.LogInformation("Creating item filter {FilterName} for realm {Realm}",
                request.FilterName, request.Realm);

            var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when creating item filter (OAuth token required)");
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when creating item filter (check OAuth scopes)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<ItemFilterResponse>(_jsonOptions, cancellationToken);
            var filter = data?.Filter;

            if (filter != null)
            {
                _logger.LogInformation("Successfully created item filter {FilterId}", filter.Id);
            }

            return filter;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating item filter: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item filter: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ItemFilter?> UpdateItemFilterAsync(
        string filterId,
        UpdateItemFilterRequest request,
        bool validate = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterId);
        ArgumentNullException.ThrowIfNull(request);

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-api", cancellationToken);

        try
        {
            var url = $"/item-filter/{filterId}";
            if (validate)
            {
                url += "?validate=true";
            }

            _logger.LogInformation("Updating item filter {FilterId}", filterId);

            var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item filter {FilterId} not found", filterId);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized when updating item filter (OAuth token required)");
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden when updating item filter (check OAuth scopes)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<ItemFilterResponse>(_jsonOptions, cancellationToken);
            var filter = data?.Filter;

            if (filter != null)
            {
                _logger.LogInformation("Successfully updated item filter {FilterId}", filterId);
            }

            return filter;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating item filter {FilterId}: {Message}", filterId, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item filter {FilterId}: {Message}", filterId, ex.Message);
            return null;
        }
    }
}

