namespace Poe2Mcp.Core.Models;

/// <summary>
/// Represents an item with all its properties
/// </summary>
public class ItemData
{
    public int Slot { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeLine { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public int Rarity { get; set; }
    public bool Corrupted { get; set; }
    public string Icon { get; set; } = string.Empty;
    public ItemMods Mods { get; set; } = new();
    public List<SocketGroup> Sockets { get; set; } = new();
    public List<PropertyData> Properties { get; set; } = new();
    public List<RequirementData> Requirements { get; set; } = new();
}

/// <summary>
/// Item modifiers organized by type
/// </summary>
public class ItemMods
{
    public List<string> Implicit { get; set; } = new();
    public List<string> Explicit { get; set; } = new();
    public List<string> Crafted { get; set; } = new();
    public List<string> Enchant { get; set; } = new();
    public List<string> Fractured { get; set; } = new();
}

/// <summary>
/// Socket group for items
/// </summary>
public class SocketGroup
{
    public int Group { get; set; }
    public List<Socket> Sockets { get; set; } = new();
}

/// <summary>
/// Individual socket in an item
/// </summary>
public class Socket
{
    public string Colour { get; set; } = string.Empty;
    public int Group { get; set; }
}

/// <summary>
/// Item property (like armor value, weapon damage, etc.)
/// </summary>
public class PropertyData
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
    public int DisplayMode { get; set; }
}

/// <summary>
/// Item requirement (level, stats)
/// </summary>
public class RequirementData
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int DisplayMode { get; set; }
}

/// <summary>
/// Trade API item listing
/// </summary>
public class TradeItemListing
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
    public bool Corrupted { get; set; }
    public TradePrice Price { get; set; } = new();
    public TradeSeller Seller { get; set; } = new();
    public List<PropertyData> Properties { get; set; } = new();
    public List<RequirementData> Requirements { get; set; } = new();
    public List<string> ExplicitMods { get; set; } = new();
    public List<string> ImplicitMods { get; set; } = new();
    public List<string> EnchantMods { get; set; } = new();
    public List<SocketGroup> Sockets { get; set; } = new();
    public int Links { get; set; }
    public DateTime? ListedTime { get; set; }
}

/// <summary>
/// Price information for trade listing
/// </summary>
public class TradePrice
{
    public double? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Seller information for trade listing
/// </summary>
public class TradeSeller
{
    public string Account { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public bool Online { get; set; }
}

/// <summary>
/// poe.ninja item price data
/// </summary>
public class NinjaItemPrice
{
    public string Name { get; set; } = string.Empty;
    public string BaseType { get; set; } = string.Empty;
    public double ChaosValue { get; set; }
    public double ExaltedValue { get; set; }
    public int Count { get; set; }
    public List<string> Variant { get; set; } = new();
    public string Icon { get; set; } = string.Empty;
    public int Links { get; set; }
    public int ItemLevel { get; set; }
    public SparklineData Sparkline { get; set; } = new();
}

/// <summary>
/// Price history sparkline
/// </summary>
public class SparklineData
{
    public List<double> TotalChange { get; set; } = new();
    public List<double> Data { get; set; } = new();
}
