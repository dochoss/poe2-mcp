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

        // Configure base URL
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

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
            var url = $"/character/{accountName}/{characterName}";
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

            response.EnsureSuccessStatusCode();

            var characterData = await response.Content.ReadFromJsonAsync<CharacterData>(_jsonOptions, cancellationToken);

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
            var url = $"/account/{accountName}/characters";
            _logger.LogInformation("Fetching characters for account {Account}", accountName);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var characters = await response.Content.ReadFromJsonAsync<List<CharacterData>>(_jsonOptions, cancellationToken)
                ?? new List<CharacterData>();

            // Cache the result
            await _cacheService.SetAsync(
                cacheKey,
                characters,
                TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} characters for account {Account}", characters.Count, accountName);

            return characters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account characters for {Account}: {Message}", accountName, ex.Message);
            return Array.Empty<CharacterData>();
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
