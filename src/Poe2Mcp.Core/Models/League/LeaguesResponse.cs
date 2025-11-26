namespace Poe2Mcp.Core.Models.League;

/// <summary>
/// Response from GET /league endpoint
/// </summary>
public class LeaguesResponse
{
    public List<League> Leagues { get; set; } = new();
}
