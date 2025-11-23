namespace Poe2Mcp.Core.Models;

/// <summary>
/// Represents skill data from character
/// </summary>
public class SkillData
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string SocketGroup { get; set; } = string.Empty;
    public List<SkillGemData> Gems { get; set; } = new();
    public List<SkillDpsEntry> Dps { get; set; } = new();
}

/// <summary>
/// Individual skill gem
/// </summary>
public class SkillGemData
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Quality { get; set; }
    public bool IsSupport { get; set; }
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// DPS entry for a skill
/// </summary>
public class SkillDpsEntry
{
    public string Name { get; set; } = string.Empty;
    public double Dps { get; set; }
    public double DotDps { get; set; }
    public List<double> DamageTypes { get; set; } = new();
}

/// <summary>
/// Passive tree data
/// </summary>
public class PassiveTreeData
{
    public List<PassiveNodeData> Nodes { get; set; } = new();
    public List<PassiveConnectionData> Connections { get; set; } = new();
    public Dictionary<string, PassiveGroupData> Groups { get; set; } = new();
}

/// <summary>
/// Individual passive node
/// </summary>
public class PassiveNodeData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsKeystone { get; set; }
    public bool IsNotable { get; set; }
    public bool IsMastery { get; set; }
    public List<string> Stats { get; set; } = new();
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Connection between passive nodes
/// </summary>
public class PassiveConnectionData
{
    public int From { get; set; }
    public int To { get; set; }
}

/// <summary>
/// Group of passive nodes
/// </summary>
public class PassiveGroupData
{
    public int X { get; set; }
    public int Y { get; set; }
    public List<int> Nodes { get; set; } = new();
}
