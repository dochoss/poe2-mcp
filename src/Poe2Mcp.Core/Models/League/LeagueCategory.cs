namespace Poe2Mcp.Core.Models.League;

/// <summary>
/// League category information
/// </summary>
public class LeagueCategory
{
    /// <summary>
    /// Category identifier (e.g., "Affliction")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the current/active challenge league
    /// </summary>
    public bool? Current { get; set; }
}
