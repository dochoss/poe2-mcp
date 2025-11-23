using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Fetches character data from multiple sources with intelligent fallback
/// No OAuth2 required - uses public data from poe.ninja and ladder API
/// </summary>
public class CharacterFetcher : ICharacterFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CharacterFetcher> _logger;
    private readonly ICacheService _cacheService;
    private readonly IRateLimiter _rateLimiter;
    private readonly IPoeNinjaApiClient _ninjaClient;
    private readonly JsonSerializerOptions _jsonOptions;

    private static readonly Dictionary<string, string> LeagueNameMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Rise of the Abyssal", "Abyss" },
        { "Abyss", "Abyss" },
        { "Abyss Hardcore", "Hardcore Abyss" },
        { "Abyss SSF", "SSF Abyss" },
        { "Abyss Hardcore SSF", "SSF Hardcore Abyss" }
    };

    public CharacterFetcher(
        HttpClient httpClient,
        ILogger<CharacterFetcher> logger,
        ICacheService cacheService,
        IRateLimiter rateLimiter,
        IPoeNinjaApiClient ninjaClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _ninjaClient = ninjaClient ?? throw new ArgumentNullException(nameof(ninjaClient));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Configure for ladder API
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://www.pathofexile.com/api");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }
    }

    /// <inheritdoc/>
    public async Task<CharacterData?> GetCharacterAsync(
        string accountName,
        string characterName,
        string league = "Standard",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        _logger.LogInformation("Fetching character {Character} for account {Account} (league: {League})",
            characterName, accountName, league);

        // Try poe.ninja API first (most reliable)
        try
        {
            _logger.LogDebug("Trying poe.ninja API with league={League}", league);
            var charData = await _ninjaClient.GetCharacterAsync(accountName, characterName, league, cancellationToken);
            if (charData != null && charData.Level > 0)
            {
                _logger.LogInformation("Successfully fetched from poe.ninja API");
                return charData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "poe.ninja API error: {Message}", ex.Message);
        }

        // Fallback to ladder API
        try
        {
            var charData = await GetCharacterFromLadderAsync(characterName, league, cancellationToken);
            if (charData != null)
            {
                _logger.LogInformation("Successfully fetched from ladder API");
                return charData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ladder API error: {Message}", ex.Message);
        }

        // All methods exhausted
        _logger.LogError(
            "Character '{Character}' not found after trying all sources (account: {Account}, league: {League}). " +
            "Verify the character exists and is public.",
            characterName, accountName, league);

        return null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LadderEntry>> GetTopLadderCharactersAsync(
        string league = "Standard",
        int limit = 100,
        int minLevel = 1,
        string? classFilter = null,
        CancellationToken cancellationToken = default)
    {
        var apiLeague = NormalizeLeagueName(league);
        var cacheKey = $"top_ladder:{apiLeague}:{limit}:{minLevel}:{classFilter}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<List<LadderEntry>>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            _logger.LogInformation("Cache hit for top ladder characters");
            return cachedData;
        }

        try
        {
            var baseUrl = $"/ladders/{apiLeague}";
            var topCharacters = new List<LadderEntry>();

            // Fetch ladder pages until we have enough characters
            var offset = 0;
            while (topCharacters.Count < limit && offset < 1000)
            {
                await _rateLimiter.WaitAsync("poe-ladder", cancellationToken);

                var url = $"{baseUrl}?limit=200&offset={offset}";
                _logger.LogInformation("Fetching ladder page: offset={Offset}", offset);

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<LadderResponse>(_jsonOptions, cancellationToken);
                var entries = data?.Entries ?? Array.Empty<LadderEntryRaw>();

                if (!entries.Any())
                {
                    break; // No more entries
                }

                foreach (var entry in entries)
                {
                    var charLevel = entry.Character?.Level ?? 0;
                    var charClass = entry.Character?.Class ?? "";

                    // Apply filters
                    if (charLevel < minLevel)
                    {
                        continue;
                    }

                    if (classFilter != null && !charClass.Equals(classFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    topCharacters.Add(new LadderEntry
                    {
                        Account = entry.Account?.Name ?? "",
                        Character = entry.Character?.Name ?? "",
                        Level = charLevel,
                        Class = charClass,
                        Rank = entry.Rank,
                        Dead = entry.Dead,
                        Online = entry.Online,
                        Experience = entry.Character?.Experience ?? 0
                    });

                    if (topCharacters.Count >= limit)
                    {
                        break;
                    }
                }

                offset += 200;
            }

            _logger.LogInformation("Found {Count} characters from ladder", topCharacters.Count);

            // Cache for 30 minutes
            if (topCharacters.Any())
            {
                await _cacheService.SetAsync(cacheKey, topCharacters, TimeSpan.FromMinutes(30), cancellationToken);
            }

            return topCharacters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top ladder characters: {Message}", ex.Message);
            return Array.Empty<LadderEntry>();
        }
    }

    private async Task<CharacterData?> GetCharacterFromLadderAsync(
        string characterName,
        string league,
        CancellationToken cancellationToken)
    {
        var apiLeague = NormalizeLeagueName(league);
        var cacheKey = $"ladder_char:{apiLeague}:{characterName}";

        // Check cache
        var cachedData = await _cacheService.GetAsync<CharacterData>(cacheKey, cancellationToken);
        if (cachedData != null)
        {
            return cachedData;
        }

        try
        {
            var baseUrl = $"/ladders/{apiLeague}";

            // Search through ladder pages to find the character
            for (var offset = 0; offset < 1000; offset += 200)
            {
                await _rateLimiter.WaitAsync("poe-ladder", cancellationToken);

                var url = $"{baseUrl}?limit=200&offset={offset}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<LadderResponse>(_jsonOptions, cancellationToken);

                // Search for the character in the ladder
                foreach (var entry in data?.Entries ?? Array.Empty<LadderEntryRaw>())
                {
                    var charName = entry.Character?.Name;
                    if (string.Equals(charName, characterName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Found character {Character} in ladder", characterName);

                        var charData = new CharacterData
                        {
                            Name = charName ?? characterName,
                            Level = entry.Character?.Level ?? 0,
                            Class = entry.Character?.Class ?? "Unknown",
                            League = league,
                            Account = entry.Account?.Name ?? "",
                            Experience = entry.Character?.Experience ?? 0,
                            Rank = entry.Rank,
                            Dead = entry.Dead,
                            Online = entry.Online,
                            Source = "ladder API"
                        };

                        // Cache for 5 minutes
                        await _cacheService.SetAsync(cacheKey, charData, TimeSpan.FromMinutes(5), cancellationToken);

                        return charData;
                    }
                }
            }

            _logger.LogWarning("Character {Character} not found in top 1000 of {League} ladder",
                characterName, apiLeague);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from ladder API ({League}): {Message}", apiLeague, ex.Message);
            return null;
        }
    }

    private static string NormalizeLeagueName(string league)
    {
        // Check exact match first
        if (LeagueNameMappings.TryGetValue(league, out var normalized))
        {
            return normalized;
        }

        // Return as-is if no mapping found (works for Standard, Hardcore, etc.)
        return league;
    }

    // Internal models for ladder API responses
    private class LadderResponse
    {
        public LadderEntryRaw[] Entries { get; set; } = Array.Empty<LadderEntryRaw>();
    }

    private class LadderEntryRaw
    {
        public int Rank { get; set; }
        public bool Dead { get; set; }
        public bool Online { get; set; }
        public LadderCharacter? Character { get; set; }
        public LadderAccount? Account { get; set; }
    }

    private class LadderCharacter
    {
        public string? Name { get; set; }
        public int Level { get; set; }
        public string? Class { get; set; }
        public long Experience { get; set; }
    }

    private class LadderAccount
    {
        public string? Name { get; set; }
    }
}
