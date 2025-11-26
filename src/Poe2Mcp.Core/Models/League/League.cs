namespace Poe2Mcp.Core.Models.League;

/// <summary>
/// League data from GET /league or GET /league/{league}
/// Official PoE API Reference: https://www.pathofexile.com/developer/docs/reference#leagues
/// </summary>
public class League
{
    /// <summary>
    /// League identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Realm (pc, xbox, sony, poe2)
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// League display name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// League description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// League category information
    /// </summary>
    public LeagueCategory? Category { get; set; }

    /// <summary>
    /// League rules (e.g., Hardcore, SSF)
    /// </summary>
    public List<LeagueRule>? Rules { get; set; }

    /// <summary>
    /// Registration start time (ISO8601)
    /// </summary>
    public string? RegisterAt { get; set; }

    /// <summary>
    /// Whether this is an event league
    /// </summary>
    public bool? Event { get; set; }

    /// <summary>
    /// Event goal description
    /// </summary>
    public string? Goal { get; set; }

    /// <summary>
    /// URL to forum thread
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// League start time (ISO8601)
    /// </summary>
    public string? StartAt { get; set; }

    /// <summary>
    /// League end time (ISO8601)
    /// </summary>
    public string? EndAt { get; set; }

    /// <summary>
    /// Whether this is a timed event
    /// </summary>
    public bool? TimedEvent { get; set; }

    /// <summary>
    /// Whether this is a score event
    /// </summary>
    public bool? ScoreEvent { get; set; }

    /// <summary>
    /// Whether this is a delve event (PoE1 only)
    /// </summary>
    public bool? DelveEvent { get; set; }

    /// <summary>
    /// Whether this is an ancestor event (PoE1 only)
    /// </summary>
    public bool? AncestorEvent { get; set; }

    /// <summary>
    /// Whether this is a league event
    /// </summary>
    public bool? LeagueEvent { get; set; }
}
