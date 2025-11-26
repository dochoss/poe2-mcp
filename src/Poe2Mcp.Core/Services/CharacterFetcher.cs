using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Fetches character data from multiple sources with intelligent fallback
/// No OAuth2 required - uses public data from poe.ninja and ladder API
/// </summary>
public class CharacterFetcher : ICharacterFetcher
{
    private readonly ILogger<CharacterFetcher> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPoeNinjaApiClient _ninjaClient;
    private readonly IPoeApiClient _poeApiClient;
    private readonly CharacterFetcherOptions _options;

    public CharacterFetcher(
        ILogger<CharacterFetcher> logger,
        ICacheService cacheService,
        IPoeNinjaApiClient ninjaClient,
        IPoeApiClient poeApiClient,
        IOptions<CharacterFetcherOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _ninjaClient = ninjaClient ?? throw new ArgumentNullException(nameof(ninjaClient));
        _poeApiClient = poeApiClient ?? throw new ArgumentNullException(nameof(poeApiClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
            var topCharacters = new List<LadderEntry>();

            // Fetch ladder pages until we have enough characters
            var offset = 0;
            while (topCharacters.Count < limit && offset < 1000)
            {
                _logger.LogInformation("Fetching ladder page: offset={Offset}", offset);

                var entries = await _poeApiClient.GetLadderEntriesAsync(apiLeague, 200, offset, cancellationToken);

                if (!entries.Any())
                {
                    break; // No more entries
                }

                foreach (var entry in entries)
                {
                    // Apply filters
                    if (entry.Level < minLevel)
                    {
                        continue;
                    }

                    if (classFilter != null && !entry.Class.Equals(classFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    topCharacters.Add(entry);

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
            var pageSize = _options.LadderPageSize;
            var maxDepth = _options.MaxLadderSearchDepth;

            // Search through ladder pages to find the character
            // Early termination if we get an empty page
            for (var offset = 0; offset < maxDepth; offset += pageSize)
            {
                var entries = await _poeApiClient.GetLadderEntriesAsync(apiLeague, pageSize, offset, cancellationToken);

                // Early termination: if we get no entries, we've reached the end of the ladder
                if (!entries.Any())
                {
                    _logger.LogDebug("Reached end of ladder at offset {Offset}", offset);
                    break;
                }

                // Search for the character in the ladder
                foreach (var entry in entries)
                {
                    if (string.Equals(entry.Character, characterName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Found character {Character} in ladder at rank {Rank}", 
                            characterName, entry.Rank);

                        var charData = new CharacterData
                        {
                            Name = entry.Character,
                            Level = entry.Level,
                            Class = entry.Class,
                            League = league,
                            Account = entry.Account,
                            Experience = entry.Experience,
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

            _logger.LogWarning("Character {Character} not found in top {MaxDepth} of {League} ladder",
                characterName, maxDepth, apiLeague);
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
        return LeagueNameNormalizer.NormalizeForOfficialApi(league);
    }
}
