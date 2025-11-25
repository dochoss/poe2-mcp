namespace Poe2Mcp.Core.Models;

/// <summary>
/// Represents complete character data from various APIs
/// </summary>
public class CharacterData
{
    public string Name { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int Level { get; set; }
    public string League { get; set; } = string.Empty;
    public long Experience { get; set; }
    
    public List<ItemData> Items { get; set; } = new();
    public List<SkillData> Skills { get; set; } = new();
    public List<int> Passives { get; set; } = new();
    public List<int> PassiveSet1 { get; set; } = new();
    public List<int> PassiveSet2 { get; set; } = new();
    public List<KeystoneData> Keystones { get; set; } = new();
    public List<FlaskData> Flasks { get; set; } = new();
    public List<JewelData> Jewels { get; set; } = new();
    
    public DefensiveStats Stats { get; set; } = new();
    public List<SkillDpsData> SkillDps { get; set; } = new();
    
    public string PathOfBuildingExport { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public bool WeaponSwapActive { get; set; }
    public int? Rank { get; set; }
    public bool Dead { get; set; }
    public bool Online { get; set; }
}

/// <summary>
/// Represents skill DPS data from character
/// </summary>
public class SkillDpsData
{
    public string SkillName { get; set; } = string.Empty;
    public double TotalDps { get; set; }
    public double DotDps { get; set; }
    public List<double> DamageTypes { get; set; } = new();
    public DamageBreakdown DamageBreakdown { get; set; } = new();
}

/// <summary>
/// Breakdown of damage by element
/// </summary>
public class DamageBreakdown
{
    public double Physical { get; set; }
    public double Fire { get; set; }
    public double Cold { get; set; }
    public double Lightning { get; set; }
    public double Chaos { get; set; }
}

/// <summary>
/// Represents a keystone passive
/// </summary>
public class KeystoneData
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Stats { get; set; } = new();
}

/// <summary>
/// Represents a flask
/// </summary>
public class FlaskData
{
    public string Name { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public List<string> Mods { get; set; } = new();
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Represents a jewel
/// </summary>
public class JewelData
{
    public string Name { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public List<string> Mods { get; set; } = new();
    public string Icon { get; set; } = string.Empty;
    public Position Position { get; set; } = new();
}

/// <summary>
/// Position in passive tree
/// </summary>
public class Position
{
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// Represents ladder entry data
/// </summary>
public class LadderEntry
{
    public string Account { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Class { get; set; } = string.Empty;
    public int Rank { get; set; }
    public bool Dead { get; set; }
    public bool Online { get; set; }
    public long Experience { get; set; }
}

/// <summary>
/// Response from the official PoE API /character endpoint
/// Returns a wrapper object with a "characters" array
/// </summary>
public class CharacterListResponse
{
    public List<CharacterData> Characters { get; set; } = new();
}

/// <summary>
/// Response from the official PoE API /character/<name> endpoint
/// Returns a wrapper object with a "character" property
/// </summary>
public class CharacterResponse
{
    public CharacterData? Character { get; set; }
}
