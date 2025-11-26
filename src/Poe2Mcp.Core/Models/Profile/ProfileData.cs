namespace Poe2Mcp.Core.Models.Profile;

/// <summary>
/// Account profile data from GET /profile
/// Official PoE API Reference: https://www.pathofexile.com/developer/docs/reference#account-profile
/// </summary>
public class ProfileData
{
    /// <summary>
    /// UUIDv4 in canonical format
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Account name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account locale (optional)
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Twitch account information if linked (optional)
    /// </summary>
    public TwitchInfo? Twitch { get; set; }
}
