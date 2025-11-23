namespace Poe2Mcp.Core.Services;

/// <summary>
/// Service for normalizing league names across different APIs
/// </summary>
public static class LeagueNameNormalizer
{
    /// <summary>
    /// League name mappings for poe.ninja API (URL slugs)
    /// </summary>
    private static readonly Dictionary<string, string> NinjaLeagueMappings = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>
    /// League name mappings for official PoE API (formal names)
    /// </summary>
    private static readonly Dictionary<string, string> OfficialApiLeagueMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Rise of the Abyssal", "Abyss" },
        { "Abyss", "Abyss" },
        { "Abyss Hardcore", "Hardcore Abyss" },
        { "Abyss SSF", "SSF Abyss" },
        { "Abyss Hardcore SSF", "SSF Hardcore Abyss" }
    };

    /// <summary>
    /// Normalize league name for poe.ninja API (converts to URL slug)
    /// </summary>
    /// <param name="league">Display league name</param>
    /// <returns>URL slug for poe.ninja</returns>
    public static string NormalizeForNinjaApi(string league)
    {
        // Check exact match first
        if (NinjaLeagueMappings.TryGetValue(league, out var slug))
        {
            return slug;
        }

        // Default: convert to lowercase and replace spaces with hyphens
        return league.ToLowerInvariant().Replace(" ", "-");
    }

    /// <summary>
    /// Normalize league name for official PoE API (converts to formal name)
    /// </summary>
    /// <param name="league">Display league name</param>
    /// <returns>Formal league name for official API</returns>
    public static string NormalizeForOfficialApi(string league)
    {
        // Check exact match first
        if (OfficialApiLeagueMappings.TryGetValue(league, out var normalized))
        {
            return normalized;
        }

        // Return as-is if no mapping found (works for Standard, Hardcore, etc.)
        return league;
    }
}
