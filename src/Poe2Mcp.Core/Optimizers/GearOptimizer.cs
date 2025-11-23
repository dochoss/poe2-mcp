using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Data.Models;
using Poe2Mcp.Core.Models;
using System.Text.Json;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Provides gear upgrade recommendations based on build goals and budget
/// </summary>
public class GearOptimizer : IGearOptimizer
{
    private readonly Poe2DbContext _dbContext;
    private readonly ILogger<GearOptimizer> _logger;

    // Budget thresholds (in chaos orbs)
    private static readonly Dictionary<BudgetTier, (double Min, double Max)> BudgetRanges = new()
    {
        { BudgetTier.Low, (0, 10) },
        { BudgetTier.Medium, (10, 100) },
        { BudgetTier.High, (100, 1000) },
        { BudgetTier.Unlimited, (1000, double.MaxValue) }
    };

    // Slot mapping for different naming conventions
    private static readonly Dictionary<GearSlot, List<string>> SlotMapping = new()
    {
        { GearSlot.Helmet, new() { "Helmet", "Helm" } },
        { GearSlot.BodyArmour, new() { "Body Armour", "Body Armor", "Chest" } },
        { GearSlot.Gloves, new() { "Gloves" } },
        { GearSlot.Boots, new() { "Boots" } },
        { GearSlot.Weapon, new() { "Weapon", "One Hand Axe", "One Hand Sword", "One Hand Mace", "Bow", "Staff", "Wand" } },
        { GearSlot.Offhand, new() { "Shield", "Quiver", "One Hand" } },
        { GearSlot.Amulet, new() { "Amulet" } },
        { GearSlot.Ring, new() { "Ring" } },
        { GearSlot.Belt, new() { "Belt" } }
    };

    public GearOptimizer(Poe2DbContext dbContext, ILogger<GearOptimizer> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GearOptimizationResult> OptimizeAsync(
        CharacterData characterData,
        BudgetTier budget = BudgetTier.Medium,
        OptimizationGoal goal = OptimizationGoal.Balanced,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characterData);

        _logger.LogInformation("Optimizing gear with goal={Goal}, budget={Budget}", goal, budget);

        var result = new GearOptimizationResult
        {
            BudgetTier = budget,
            Goal = goal
        };

        // Extract current items from character data
        var currentItems = ExtractCurrentItems(characterData);

        // Analyze each gear slot
        var slots = Enum.GetValues<GearSlot>();

        foreach (var slot in slots)
        {
            var upgrade = await AnalyzeSlotAsync(
                characterData,
                currentItems.GetValueOrDefault(slot),
                slot,
                budget,
                goal,
                cancellationToken);

            if (upgrade != null)
            {
                result.PriorityUpgrades.Add(upgrade);
                result.TotalEstimatedCostChaos += upgrade.EstimatedCostChaos;
            }
        }

        // Sort by priority
        result.PriorityUpgrades.Sort((a, b) => 
            GetPriorityValue(b.Priority).CompareTo(GetPriorityValue(a.Priority)));

        // Generate summary
        result.Summary = GenerateSummary(result);

        return result;
    }

    private Dictionary<GearSlot, ItemData?> ExtractCurrentItems(CharacterData characterData)
    {
        var currentItems = new Dictionary<GearSlot, ItemData?>();

        if (characterData.Items == null)
        {
            return currentItems;
        }

        foreach (var item in characterData.Items)
        {
            var slot = ParseGearSlot(item.TypeLine, item.Slot);
            if (slot.HasValue)
            {
                currentItems[slot.Value] = item;
            }
        }

        return currentItems;
    }

    private GearSlot? ParseGearSlot(string typeLine, int slotIndex)
    {
        var normalized = typeLine.ToLowerInvariant();

        if (normalized.Contains("helm")) return GearSlot.Helmet;
        if (normalized.Contains("body") || normalized.Contains("chest")) return GearSlot.BodyArmour;
        if (normalized.Contains("glove")) return GearSlot.Gloves;
        if (normalized.Contains("boot")) return GearSlot.Boots;
        if (normalized.Contains("sword") || normalized.Contains("axe") || normalized.Contains("mace") || 
            normalized.Contains("bow") || normalized.Contains("staff") || normalized.Contains("wand")) return GearSlot.Weapon;
        if (normalized.Contains("shield") || normalized.Contains("quiver")) return GearSlot.Offhand;
        if (normalized.Contains("amulet")) return GearSlot.Amulet;
        if (normalized.Contains("ring")) return GearSlot.Ring;
        if (normalized.Contains("belt")) return GearSlot.Belt;

        return null;
    }

    private async Task<GearUpgradeRecommendation?> AnalyzeSlotAsync(
        CharacterData characterData,
        ItemData? currentItem,
        GearSlot slot,
        BudgetTier budget,
        OptimizationGoal goal,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine priority based on current item quality
            var priority = DeterminePriority(currentItem, slot, goal);

            if (priority == UpgradePriority.None)
            {
                return null;
            }

            // Find suitable upgrade items from database
            var suggestedItems = await FindUpgradeItemsAsync(
                currentItem,
                slot,
                budget,
                goal,
                characterData.Level,
                cancellationToken);

            if (suggestedItems.Count == 0)
            {
                return null;
            }

            // Pick best suggestion
            var bestSuggestion = suggestedItems[0];

            // Calculate improvement estimate
            var improvement = EstimateImprovement(currentItem, bestSuggestion, goal);

            return new GearUpgradeRecommendation
            {
                Slot = slot,
                Priority = priority,
                CurrentItem = currentItem?.Name ?? "Empty",
                CurrentItemRarity = GetRarityName(currentItem?.Rarity ?? 0),
                SuggestedItem = bestSuggestion.Name,
                SuggestedItemType = bestSuggestion.ItemClass,
                ImprovementEstimate = improvement,
                EstimatedCostChaos = bestSuggestion.ChaosValue,
                Reasoning = GenerateReasoning(currentItem, bestSuggestion, goal),
                AlternativeSuggestions = suggestedItems.Skip(1).Take(3).Select(i => i.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing slot {Slot}", slot);
            return null;
        }
    }

    private UpgradePriority DeterminePriority(ItemData? currentItem, GearSlot slot, OptimizationGoal goal)
    {
        // Empty slot is always critical
        if (currentItem == null)
        {
            return UpgradePriority.Critical;
        }

        var rarity = currentItem.Rarity; // 0=Normal, 1=Magic, 2=Rare, 3=Unique
        var itemLevel = currentItem.ItemLevel;

        // Low item level is high priority
        if (itemLevel < 50)
        {
            return UpgradePriority.High;
        }

        // Normal/magic items are medium-high priority
        if (rarity <= 1) // Normal or Magic
        {
            return itemLevel >= 60 ? UpgradePriority.Medium : UpgradePriority.High;
        }

        // Rare items might need upgrades depending on mods
        if (rarity == 2) // Rare
        {
            // Check if item has good mods (simplified check)
            var modCount = currentItem.Mods.Explicit.Count + currentItem.Mods.Implicit.Count;
            if (modCount < 4)
            {
                return UpgradePriority.Medium;
            }
            return UpgradePriority.Low;
        }

        // Unique items are usually good
        if (rarity == 3) // Unique
        {
            // Check if it's a leveling unique
            var levelReq = GetLevelRequirement(currentItem.Requirements);
            if (levelReq < 60)
            {
                return UpgradePriority.Medium;
            }
            return UpgradePriority.Low;
        }

        return UpgradePriority.Low;
    }

    private int GetLevelRequirement(List<RequirementData> requirements)
    {
        var levelReq = requirements.FirstOrDefault(r => r.Name.Equals("Level", StringComparison.OrdinalIgnoreCase));
        if (levelReq != null && int.TryParse(levelReq.Value, out var level))
        {
            return level;
        }
        return 100;
    }

    private string GetRarityName(int rarity)
    {
        return rarity switch
        {
            0 => "normal",
            1 => "magic",
            2 => "rare",
            3 => "unique",
            _ => "unknown"
        };
    }

    private async Task<List<SuggestedItem>> FindUpgradeItemsAsync(
        ItemData? currentItem,
        GearSlot slot,
        BudgetTier budget,
        OptimizationGoal goal,
        int characterLevel,
        CancellationToken cancellationToken)
    {
        var (minChaos, maxChaos) = BudgetRanges[budget];
        var suitableItems = new List<SuggestedItem>();

        try
        {
            // Get item class names for this slot
            var slotClasses = SlotMapping.GetValueOrDefault(slot, new List<string>());

            // Query unique items for this slot
            var uniqueItems = await _dbContext.UniqueItems
                .Where(u => slotClasses.Contains(u.ItemClass))
                .ToListAsync(cancellationToken);

            foreach (var item in uniqueItems)
            {
                // Filter by level requirement
                if (item.RequiredLevel > characterLevel)
                {
                    continue;
                }

                // Parse stats to get chaos value
                var stats = ParseStats(item.Stats);
                var chaosValue = stats.GetValueOrDefault("chaos_value", 0.0);

                // Filter by budget
                if (chaosValue < minChaos || chaosValue > maxChaos)
                {
                    continue;
                }

                // Score item based on goal
                var score = ScoreItem(item, stats, goal);

                suitableItems.Add(new SuggestedItem
                {
                    Name = item.Name,
                    ItemClass = item.ItemClass,
                    LevelRequirement = item.RequiredLevel ?? 0,
                    ChaosValue = chaosValue,
                    Score = score,
                    Stats = stats
                });
            }

            // Sort by score
            suitableItems.Sort((a, b) => b.Score.CompareTo(a.Score));

            return suitableItems.Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying upgrade items for slot {Slot}", slot);
            return new List<SuggestedItem>();
        }
    }

    private Dictionary<string, double> ParseStats(string? statsJson)
    {
        if (string.IsNullOrEmpty(statsJson))
        {
            return new Dictionary<string, double>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(statsJson) 
                ?? new Dictionary<string, double>();
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    private double ScoreItem(UniqueItem item, Dictionary<string, double> stats, OptimizationGoal goal)
    {
        double score = 0.0;

        switch (goal)
        {
            case OptimizationGoal.Dps:
            case OptimizationGoal.BossDamage:
            case OptimizationGoal.ClearSpeed:
                // Prioritize damage stats
                score += stats.GetValueOrDefault("physical_damage", 0) * 2;
                score += stats.GetValueOrDefault("elemental_damage", 0) * 2;
                score += stats.GetValueOrDefault("attack_speed", 0) * 5;
                score += stats.GetValueOrDefault("critical_chance", 0) * 10;
                break;

            case OptimizationGoal.Defense:
                // Prioritize defensive stats
                score += stats.GetValueOrDefault("life", 0) * 1;
                score += stats.GetValueOrDefault("energy_shield", 0) * 0.5;
                score += stats.GetValueOrDefault("armour", 0) * 0.1;
                score += stats.GetValueOrDefault("evasion", 0) * 0.1;
                score += stats.GetValueOrDefault("resistances", 0) * 2;
                break;

            case OptimizationGoal.Balanced:
            default:
                // Balance between offense and defense
                score += stats.GetValueOrDefault("physical_damage", 0) * 1;
                score += stats.GetValueOrDefault("life", 0) * 0.5;
                score += stats.GetValueOrDefault("resistances", 0) * 1.5;
                break;
        }

        // Base score on item level
        score += (item.RequiredLevel ?? 0) * 0.1;

        return score;
    }

    private double EstimateImprovement(ItemData? currentItem, SuggestedItem suggestedItem, OptimizationGoal goal)
    {
        if (currentItem == null)
        {
            return 1.0; // 100% improvement from empty
        }

        var currentRarity = currentItem.Rarity; // 0=Normal, 1=Magic, 2=Rare, 3=Unique

        // Simplified improvement estimation
        if (currentRarity <= 1) // Normal or Magic
        {
            return 0.5; // 50% improvement
        }

        if (currentRarity == 2) // Rare
        {
            return 0.2; // 20% improvement
        }

        if (currentRarity == 3) // Unique
        {
            // Unique to unique upgrade
            var currentLevel = GetLevelRequirement(currentItem.Requirements);
            var suggestedLevel = suggestedItem.LevelRequirement;

            if (suggestedLevel > currentLevel + 20)
            {
                return 0.3;
            }
            return 0.1;
        }

        return 0.15;
    }

    private string GenerateReasoning(ItemData? currentItem, SuggestedItem suggestedItem, OptimizationGoal goal)
    {
        if (currentItem == null)
        {
            return $"Empty slot - equip {suggestedItem.Name} for immediate improvement";
        }

        var reasoningParts = new List<string>();
        var currentRarity = GetRarityName(currentItem.Rarity);

        if (currentRarity == "normal" || currentRarity == "magic")
        {
            reasoningParts.Add("Current item is low quality");
        }

        switch (goal)
        {
            case OptimizationGoal.Dps:
            case OptimizationGoal.BossDamage:
            case OptimizationGoal.ClearSpeed:
                reasoningParts.Add($"{suggestedItem.Name} provides better offensive stats");
                break;
            case OptimizationGoal.Defense:
                reasoningParts.Add($"{suggestedItem.Name} provides better defensive stats");
                break;
            default:
                reasoningParts.Add($"{suggestedItem.Name} offers better overall stats");
                break;
        }

        if (suggestedItem.ChaosValue > 0)
        {
            reasoningParts.Add($"(~{suggestedItem.ChaosValue:F1} chaos)");
        }

        return string.Join(". ", reasoningParts);
    }

    private string GenerateSummary(GearOptimizationResult result)
    {
        var numUpgrades = result.PriorityUpgrades.Count;
        if (numUpgrades == 0)
        {
            return "Your gear is well-optimized for your build!";
        }

        var criticalCount = result.PriorityUpgrades.Count(u => u.Priority == UpgradePriority.Critical);
        var highCount = result.PriorityUpgrades.Count(u => u.Priority == UpgradePriority.High);

        var summary = $"Found {numUpgrades} potential upgrades for {result.Goal} optimization. ";

        if (criticalCount > 0)
        {
            summary += $"{criticalCount} critical upgrades needed. ";
        }
        if (highCount > 0)
        {
            summary += $"{highCount} high priority upgrades. ";
        }

        summary += $"Estimated total cost: {result.TotalEstimatedCostChaos:F1} chaos orbs.";

        return summary;
    }

    private int GetPriorityValue(UpgradePriority priority)
    {
        return priority switch
        {
            UpgradePriority.Critical => 4,
            UpgradePriority.High => 3,
            UpgradePriority.Medium => 2,
            UpgradePriority.Low => 1,
            _ => 0
        };
    }

    private class SuggestedItem
    {
        public string Name { get; set; } = string.Empty;
        public string ItemClass { get; set; } = string.Empty;
        public int LevelRequirement { get; set; }
        public double ChaosValue { get; set; }
        public double Score { get; set; }
        public Dictionary<string, double> Stats { get; set; } = new();
    }
}
