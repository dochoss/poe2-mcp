namespace Poe2Mcp.Core.Models.League;

/// <summary>
/// League rule (e.g., Hardcore, SSF)
/// </summary>
public class LeagueRule
{
    /// <summary>
    /// Rule identifier (e.g., "Hardcore", "NoParties" for SSF)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Rule display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description
    /// </summary>
    public string? Description { get; set; }
}
