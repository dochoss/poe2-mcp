using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for the official Path of Exile API
/// Implements OAuth 2.0, rate limiting, and caching
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
    public async Task<PassiveTreeData> GetPassiveTreeAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "passive_tree_data";

        // Check cache (passive tree data is static, cache for long time)
        var cachedData = await _cacheService.GetAsync<PassiveTreeData>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for passive tree data");
            return cachedData;
        }

        try
        {
            const string url = "/passive-tree";
            _logger.LogInformation("Fetching passive tree data");

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var treeData = await response.Content.ReadFromJsonAsync<PassiveTreeData>(_jsonOptions, cancellationToken)
                ?? new PassiveTreeData();

            // Cache for 24 hours (passive tree rarely changes)
            await _cacheService.SetAsync(
                cacheKey,
                treeData,
                TimeSpan.FromHours(24),
                cancellationToken);

            _logger.LogInformation("Successfully fetched passive tree data");

            return treeData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching passive tree: {Message}", ex.Message);
            return new PassiveTreeData();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetItemsDataAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "items_data";

        // Check cache
        var cachedData = await _cacheService.GetAsync<Dictionary<string, object>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for items data");
            return cachedData;
        }

        try
        {
            const string url = "/items";
            _logger.LogInformation("Fetching items data");

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var itemsData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(_jsonOptions, cancellationToken)
                ?? new Dictionary<string, object>();

            // Cache for 24 hours
            await _cacheService.SetAsync(
                cacheKey,
                itemsData,
                TimeSpan.FromHours(24),
                cancellationToken);

            _logger.LogInformation("Successfully fetched items data");

            return itemsData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching items data: {Message}", ex.Message);
            return new Dictionary<string, object>();
        }
    }
}
