using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for poe.ninja API with web scraping fallback
/// Fetches character data, build rankings, and economy data
/// </summary>
public class PoeNinjaApiClient : IPoeNinjaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PoeNinjaApiClient> _logger;
    private readonly ICacheService _cacheService;
    private readonly IRateLimiter _rateLimiter;
    private readonly PoeNinjaOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    private static readonly Dictionary<string, string> LeagueMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Rise of the Abyssal", "abyss" },
        { "Abyss", "abyss" },
        { "Abyss Hardcore", "abysshc" },
        { "Abyss HC", "abysshc" },
        { "Abyss SSF", "abyssssf" },
        { "Abyss HC SSF", "abysshcssf" },
        { "Abyss Hardcore SSF", "abysshcssf" },
        { "Standard", "standard" },
        { "Hardcore", "hardcore" },
        { "SSF Standard", "ssf-standard" },
        { "SSF Hardcore", "ssf-hardcore" }
    };

    public PoeNinjaApiClient(
        HttpClient httpClient,
        ILogger<PoeNinjaApiClient> logger,
        ICacheService cacheService,
        IRateLimiter rateLimiter,
        IOptions<PoeNinjaOptions> options)
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

        // Configure headers
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PoE2-MCP-Server/1.0");
        }
        if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
        {
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html");
        }
    }

    /// <inheritdoc/>
    public async Task<CharacterData?> GetCharacterAsync(
        string account,
        string character,
        string league = "Abyss",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(character);

        var cacheKey = $"ninja_character_{account}_{character}_{league}";

        // Check cache first
        var cachedData = await _cacheService.GetAsync<CharacterData>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for character {Character} ({League})", character, league);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-ninja", cancellationToken);

        try
        {
            _logger.LogInformation("Fetching character: {Character} (Account: {Account}, League: {League})",
                character, account, league);

            // Get index state to find snapshot version
            var indexState = await GetIndexStateAsync(cancellationToken);
            if (indexState == null)
            {
                _logger.LogWarning("Could not get index state");
                return null;
            }

            // Find snapshot for league
            var leagueSlug = GetLeagueSlug(league);
            var snapshot = FindSnapshotForLeague(indexState, leagueSlug);

            if (snapshot == null)
            {
                _logger.LogWarning("No snapshot found for league '{League}' (slug: '{Slug}')", league, leagueSlug);
                return null;
            }

            var version = snapshot["version"]?.GetValue<string>();
            var overview = snapshot["snapshotName"]?.GetValue<string>();

            _logger.LogInformation("Using snapshot version: {Version}, overview: {Overview}", version, overview);

            // Call character API
            var url = $"/poe2/api/builds/{version}/character";
            var queryParams = $"?account={Uri.EscapeDataString(account)}&name={Uri.EscapeDataString(character)}&overview={Uri.EscapeDataString(overview ?? "")}";

            var response = await _httpClient.GetAsync(url + queryParams, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Character not found (404)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var apiData = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);
            if (apiData == null)
            {
                return null;
            }

            var charData = NormalizeApiCharacterData(apiData, account, character, league);

            // Cache the result
            if (charData != null)
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    charData,
                    TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                    cancellationToken);

                _logger.LogInformation("Successfully fetched and cached character {Character}", character);
            }

            return charData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching character from poe.ninja: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CharacterData>> GetTopBuildsAsync(
        string league = "Standard",
        string? className = null,
        string? skill = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var leagueSlug = GetLeagueSlug(league);
        var cacheKey = $"ninja_top_builds_{leagueSlug}_{className}_{skill}_{limit}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<CharacterData>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for top builds");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-ninja", cancellationToken);

        try
        {
            var url = $"/poe2/builds/{leagueSlug}";
            _logger.LogInformation("Fetching top builds from: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("poe.ninja builds page returned {StatusCode} for league '{League}'",
                    response.StatusCode, leagueSlug);
                return Array.Empty<CharacterData>();
            }

            // For now, return empty list
            // Full implementation would parse HTML/JSON from the page
            _logger.LogWarning("HTML parsing not yet implemented for top builds");
            return Array.Empty<CharacterData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top builds: {Message}", ex.Message);
            return Array.Empty<CharacterData>();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NinjaItemPrice>> GetItemPricesAsync(
        string league = "Standard",
        string itemType = "UniqueWeapon",
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ninja_prices_{league}_{itemType}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<NinjaItemPrice>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for item prices");
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-ninja", cancellationToken);

        try
        {
            var url = $"/api/data/itemoverview?league={Uri.EscapeDataString(league)}&type={Uri.EscapeDataString(itemType)}";
            _logger.LogInformation("Fetching item prices");

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Item prices API returned {StatusCode}", response.StatusCode);
                return Array.Empty<NinjaItemPrice>();
            }

            var data = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);
            var lines = data?["lines"]?.AsArray();

            if (lines == null)
            {
                return Array.Empty<NinjaItemPrice>();
            }

            var items = lines
                .Where(line => line != null)
                .Select(line => new NinjaItemPrice
                {
                    Name = line!["name"]?.GetValue<string>() ?? "",
                    BaseType = line["baseType"]?.GetValue<string>() ?? "",
                    ChaosValue = line["chaosValue"]?.GetValue<double>() ?? 0,
                    ExaltedValue = line["exaltedValue"]?.GetValue<double>() ?? 0,
                    Count = line["count"]?.GetValue<int>() ?? 0,
                    Icon = line["icon"]?.GetValue<string>() ?? "",
                    Links = line["links"]?.GetValue<int>() ?? 0,
                    ItemLevel = line["itemLevel"]?.GetValue<int>() ?? 0
                })
                .ToList();

            // Cache the result
            await _cacheService.SetAsync(
                cacheKey,
                items,
                TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                cancellationToken);

            _logger.LogInformation("Successfully fetched {Count} item prices", items.Count);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item prices: {Message}", ex.Message);
            return Array.Empty<NinjaItemPrice>();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetPobImportAsync(
        string account,
        string character,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(character);

        var cacheKey = $"ninja_pob_{account}_{character}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for PoB code: {Character}", character);
            return cachedData;
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync("poe-ninja", cancellationToken);

        try
        {
            _logger.LogInformation("Fetching PoB code for character: {Character} (Account: {Account})",
                character, account);

            var url = $"/poe2/api/builds/pob/import?accountName={Uri.EscapeDataString(account)}&characterName={Uri.EscapeDataString(character)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Character not found for PoB import (404)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);

            var pobCode = data?["pob"]?.GetValue<string>()
                ?? data?["code"]?.GetValue<string>()
                ?? data?["build"]?.GetValue<string>();

            if (pobCode != null)
            {
                // Cache the result
                await _cacheService.SetAsync(
                    cacheKey,
                    pobCode,
                    TimeSpan.FromSeconds(_options.CacheTtlSeconds),
                    cancellationToken);

                _logger.LogInformation("Successfully fetched PoB code for {Character}", character);
            }

            return pobCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PoB import API failed: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<JsonObject?> GetIndexStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = "/poe2/api/data/index-state";
            _logger.LogDebug("Fetching index state from: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Index state returned {StatusCode}", response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions, cancellationToken);
            _logger.LogDebug("Got index state with {Count} snapshot versions",
                data?["snapshotVersions"]?.AsArray()?.Count ?? 0);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch index state: {Message}", ex.Message);
            return null;
        }
    }

    private static string GetLeagueSlug(string league)
    {
        // Check exact match first
        if (LeagueMappings.TryGetValue(league, out var slug))
        {
            return slug;
        }

        // Default: convert to lowercase and replace spaces with hyphens
        return league.ToLowerInvariant().Replace(" ", "-");
    }

    private JsonObject? FindSnapshotForLeague(JsonObject indexState, string leagueSlug)
    {
        var snapshots = indexState["snapshotVersions"]?.AsArray();
        if (snapshots == null)
        {
            return null;
        }

        return snapshots
            .Where(snapshot => snapshot?["url"]?.GetValue<string>() == leagueSlug)
            .Select(snapshot => snapshot!.AsObject())
            .FirstOrDefault();
    }

    private CharacterData NormalizeApiCharacterData(JsonObject apiData, string account, string character, string league)
    {
        var defensiveStats = apiData["defensiveStats"]?.AsObject();

        var stats = new DefensiveStats
        {
            Life = defensiveStats?["life"]?.GetValue<int>() ?? 0,
            EnergyShield = defensiveStats?["energyShield"]?.GetValue<int>() ?? 0,
            Mana = defensiveStats?["mana"]?.GetValue<int>() ?? 0,
            Spirit = defensiveStats?["spirit"]?.GetValue<int>() ?? 0,
            Evasion = defensiveStats?["evasionRating"]?.GetValue<int>() ?? 0,
            Armor = defensiveStats?["armour"]?.GetValue<int>() ?? 0,
            Strength = defensiveStats?["strength"]?.GetValue<int>() ?? 0,
            Dexterity = defensiveStats?["dexterity"]?.GetValue<int>() ?? 0,
            Intelligence = defensiveStats?["intelligence"]?.GetValue<int>() ?? 0,
            FireResistance = defensiveStats?["fireResistance"]?.GetValue<int>() ?? 0,
            ColdResistance = defensiveStats?["coldResistance"]?.GetValue<int>() ?? 0,
            LightningResistance = defensiveStats?["lightningResistance"]?.GetValue<int>() ?? 0,
            ChaosResistance = defensiveStats?["chaosResistance"]?.GetValue<int>() ?? 0,
            BlockChance = defensiveStats?["blockChance"]?.GetValue<double>() ?? 0,
            SpellBlockChance = defensiveStats?["spellBlockChance"]?.GetValue<double>() ?? 0,
            SpellSuppression = defensiveStats?["spellSuppressionChance"]?.GetValue<double>() ?? 0,
            MovementSpeed = defensiveStats?["movementSpeed"]?.GetValue<double>() ?? 0
        };

        var charData = new CharacterData
        {
            Name = apiData["name"]?.GetValue<string>() ?? character,
            Account = apiData["account"]?.GetValue<string>() ?? account,
            Class = apiData["class"]?.GetValue<string>() ?? "Unknown",
            Level = apiData["level"]?.GetValue<int>() ?? 0,
            League = apiData["league"]?.GetValue<string>() ?? league,
            Stats = stats,
            PathOfBuildingExport = apiData["pathOfBuildingExport"]?.GetValue<string>() ?? "",
            Source = "poe.ninja API",
            FetchedAt = DateTime.UtcNow,
            WeaponSwapActive = apiData["useSecondWeaponSet"]?.GetValue<bool>() ?? false
        };

        // Extract passive allocations
        var passives = apiData["passiveSelection"]?.AsArray();
        if (passives != null)
        {
            charData.Passives = passives
                .Where(p => p != null)
                .Select(p => p!.GetValue<int>())
                .ToList();
        }

        return charData;
    }
}
