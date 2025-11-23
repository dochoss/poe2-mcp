using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Main gear evaluation engine.
/// Uses calculator modules to precisely quantify gear upgrade value.
/// </summary>
public class GearEvaluator : IGearEvaluator
{
    private readonly ILogger<GearEvaluator> _logger;
    private readonly IEhpCalculator _ehpCalculator;
    private readonly IDamageCalculator? _damageCalculator;

    public GearEvaluator(
        ILogger<GearEvaluator> logger,
        IEhpCalculator ehpCalculator,
        IDamageCalculator? damageCalculator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ehpCalculator = ehpCalculator ?? throw new ArgumentNullException(nameof(ehpCalculator));
        _damageCalculator = damageCalculator;
        _logger.LogInformation("GearEvaluator initialized");
    }

    public UpgradeValue EvaluateUpgrade(
        GearStats currentGear,
        GearStats upgradeGear,
        CharacterStats baseCharacterStats,
        ThreatProfile? threatProfile = null,
        double? priceChaos = null)
    {
        ArgumentNullException.ThrowIfNull(currentGear);
        ArgumentNullException.ThrowIfNull(upgradeGear);
        ArgumentNullException.ThrowIfNull(baseCharacterStats);

        _logger.LogInformation(
            "Evaluating upgrade: {Current} -> {Upgrade}",
            currentGear.ItemName, upgradeGear.ItemName);

        threatProfile ??= new ThreatProfile();
        var warnings = new List<string>();

        // Calculate total stats with current gear
        var currentTotal = CombineStats(baseCharacterStats, currentGear);

        // Calculate total stats with upgrade gear
        var upgradeTotal = CombineStats(baseCharacterStats, upgradeGear);

        // Calculate EHP changes
        var ehpChanges = CalculateEhpChanges(currentTotal, upgradeTotal, threatProfile);

        // Calculate DPS changes (if damage calculator available)
        var dpsChange = CalculateDpsChanges(currentTotal, upgradeTotal);

        // Calculate resistance changes
        var resistanceChanges = new Dictionary<string, double>
        {
            ["fire"] = upgradeGear.FireRes - currentGear.FireRes,
            ["cold"] = upgradeGear.ColdRes - currentGear.ColdRes,
            ["lightning"] = upgradeGear.LightningRes - currentGear.LightningRes,
            ["chaos"] = upgradeGear.ChaosRes - currentGear.ChaosRes
        };

        // Calculate raw stat changes
        var statChanges = new Dictionary<string, double>
        {
            ["life"] = upgradeGear.Life - currentGear.Life,
            ["mana"] = upgradeGear.Mana - currentGear.Mana,
            ["armor"] = upgradeGear.Armor - currentGear.Armor,
            ["evasion"] = upgradeGear.Evasion - currentGear.Evasion,
            ["energy_shield"] = upgradeGear.EnergyShield - currentGear.EnergyShield,
            ["spirit"] = upgradeGear.Spirit - currentGear.Spirit,
            ["strength"] = upgradeGear.Strength - currentGear.Strength,
            ["dexterity"] = upgradeGear.Dexterity - currentGear.Dexterity,
            ["intelligence"] = upgradeGear.Intelligence - currentGear.Intelligence
        };

        // Check for warnings
        warnings.AddRange(CheckUpgradeWarnings(currentTotal, upgradeTotal, resistanceChanges));

        // Calculate priority score
        var priorityScore = CalculatePriorityScore(
            ehpChanges,
            dpsChange,
            resistanceChanges,
            statChanges,
            currentTotal,
            priceChaos);

        // Generate recommendation
        var recommendation = GenerateRecommendation(
            priorityScore,
            ehpChanges,
            dpsChange,
            warnings);

        var result = new UpgradeValue
        {
            EhpChanges = ehpChanges,
            DpsChange = dpsChange,
            ResistanceChanges = resistanceChanges,
            StatChanges = statChanges,
            PriorityScore = priorityScore,
            Recommendation = recommendation,
            TradeValue = priceChaos,
            Warnings = warnings
        };

        _logger.LogInformation(
            "Upgrade evaluation complete: {Recommendation} (priority: {Score:F1})",
            recommendation, priorityScore);

        return result;
    }

    public IReadOnlyList<(GearStats Gear, UpgradeValue Value)> EvaluateMultipleUpgrades(
        GearStats currentGear,
        IEnumerable<(GearStats Gear, double? Price)> potentialUpgrades,
        CharacterStats baseCharacterStats,
        int topN = 5)
    {
        ArgumentNullException.ThrowIfNull(currentGear);
        ArgumentNullException.ThrowIfNull(potentialUpgrades);
        ArgumentNullException.ThrowIfNull(baseCharacterStats);

        var upgradesList = potentialUpgrades.ToList();
        _logger.LogInformation("Evaluating {Count} upgrade options", upgradesList.Count);

        var results = new List<(GearStats Gear, UpgradeValue Value)>();

        foreach (var (gear, price) in upgradesList)
        {
            var value = EvaluateUpgrade(
                currentGear,
                gear,
                baseCharacterStats,
                priceChaos: price);
            results.Add((gear, value));
        }

        // Sort by priority score (descending)
        results.Sort((a, b) => b.Value.PriorityScore.CompareTo(a.Value.PriorityScore));

        // Return top N
        var topResults = results.Take(topN).ToList();

        if (topResults.Count > 0)
        {
            _logger.LogInformation(
                "Top {Count} upgrades identified (best score: {Score:F1})",
                topResults.Count, topResults[0].Value.PriorityScore);
        }

        return topResults;
    }

    public ItemComparison CompareItems(
        GearStats itemA,
        GearStats itemB,
        CharacterStats baseCharacterStats)
    {
        ArgumentNullException.ThrowIfNull(itemA);
        ArgumentNullException.ThrowIfNull(itemB);
        ArgumentNullException.ThrowIfNull(baseCharacterStats);

        // Evaluate both as upgrades from a "null" item
        var nullItem = new GearStats
        {
            ItemName = "None",
            ItemSlot = itemA.ItemSlot
        };

        var valueA = EvaluateUpgrade(nullItem, itemA, baseCharacterStats);
        var valueB = EvaluateUpgrade(nullItem, itemB, baseCharacterStats);

        // Determine winner
        string winner;
        double scoreDiff;

        if (valueA.PriorityScore > valueB.PriorityScore)
        {
            winner = itemA.ItemName;
            scoreDiff = valueA.PriorityScore - valueB.PriorityScore;
        }
        else if (valueB.PriorityScore > valueA.PriorityScore)
        {
            winner = itemB.ItemName;
            scoreDiff = valueB.PriorityScore - valueA.PriorityScore;
        }
        else
        {
            winner = "Tie";
            scoreDiff = 0.0;
        }

        return new ItemComparison
        {
            ItemA = itemA.ItemName,
            ItemAScore = valueA.PriorityScore,
            ItemAValue = valueA,
            ItemB = itemB.ItemName,
            ItemBScore = valueB.PriorityScore,
            ItemBValue = valueB,
            Winner = winner,
            ScoreDifference = scoreDiff
        };
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private CharacterStats CombineStats(CharacterStats baseStats, GearStats gear)
    {
        return new CharacterStats
        {
            Armor = baseStats.Armor + gear.Armor,
            Evasion = baseStats.Evasion + gear.Evasion,
            EnergyShield = baseStats.EnergyShield + gear.EnergyShield,
            Life = baseStats.Life + gear.Life,
            Mana = baseStats.Mana + gear.Mana,
            FireRes = baseStats.FireRes + gear.FireRes,
            ColdRes = baseStats.ColdRes + gear.ColdRes,
            LightningRes = baseStats.LightningRes + gear.LightningRes,
            ChaosRes = baseStats.ChaosRes + gear.ChaosRes,
            BlockChance = baseStats.BlockChance + gear.BlockChance,
            Strength = baseStats.Strength + gear.Strength,
            Dexterity = baseStats.Dexterity + gear.Dexterity,
            Intelligence = baseStats.Intelligence + gear.Intelligence,
            IncreasedDamage = baseStats.IncreasedDamage + gear.IncreasedDamage,
            MoreDamage = baseStats.MoreDamage * (gear.MoreDamage > 0 ? gear.MoreDamage : 1.0),
            AddedFlatDamage = baseStats.AddedFlatDamage + gear.AddedFlatDamage,
            CritChance = baseStats.CritChance + gear.CritChance,
            CritMulti = baseStats.CritMulti + gear.CritMulti,
            Spirit = baseStats.Spirit + gear.Spirit
        };
    }

    private Dictionary<string, Dictionary<string, double>> CalculateEhpChanges(
        CharacterStats currentStats,
        CharacterStats upgradeStats,
        ThreatProfile threatProfile)
    {
        // Build DefensiveStats for current
        var currentDefensive = new DefensiveStats
        {
            Life = (int)currentStats.Life,
            EnergyShield = (int)currentStats.EnergyShield,
            Armor = (int)currentStats.Armor,
            Evasion = (int)currentStats.Evasion,
            BlockChance = (int)currentStats.BlockChance,
            FireResistance = (int)currentStats.FireRes,
            ColdResistance = (int)currentStats.ColdRes,
            LightningResistance = (int)currentStats.LightningRes,
            ChaosResistance = (int)currentStats.ChaosRes
        };

        // Build DefensiveStats for upgrade
        var upgradeDefensive = new DefensiveStats
        {
            Life = (int)upgradeStats.Life,
            EnergyShield = (int)upgradeStats.EnergyShield,
            Armor = (int)upgradeStats.Armor,
            Evasion = (int)upgradeStats.Evasion,
            BlockChance = (int)upgradeStats.BlockChance,
            FireResistance = (int)upgradeStats.FireRes,
            ColdResistance = (int)upgradeStats.ColdRes,
            LightningResistance = (int)upgradeStats.LightningRes,
            ChaosResistance = (int)upgradeStats.ChaosRes
        };

        // Calculate EHP for both
        var currentEhp = _ehpCalculator.CalculateEhp(currentDefensive, threatProfile);
        var upgradeEhp = _ehpCalculator.CalculateEhp(upgradeDefensive, threatProfile);

        // Calculate changes
        var changes = new Dictionary<string, Dictionary<string, double>>();

        foreach (var (damageType, currentValue) in currentEhp)
        {
            if (upgradeEhp.TryGetValue(damageType, out var upgradeValue))
            {
                var absolute = upgradeValue - currentValue;
                var percent = currentValue > 0 ? (absolute / currentValue) * 100.0 : 0.0;

                changes[damageType] = new Dictionary<string, double>
                {
                    ["current"] = currentValue,
                    ["upgrade"] = upgradeValue,
                    ["absolute"] = absolute,
                    ["percent"] = percent
                };
            }
        }

        return changes;
    }

    private Dictionary<string, double> CalculateDpsChanges(
        CharacterStats currentStats,
        CharacterStats upgradeStats)
    {
        // If no damage calculator, return relative DPS based on stats
        try
        {
            var currentDps = CalculateRelativeDps(currentStats);
            var upgradeDps = CalculateRelativeDps(upgradeStats);

            var absolute = upgradeDps - currentDps;
            var percent = currentDps > 0 ? (absolute / currentDps) * 100.0 : 0.0;

            return new Dictionary<string, double>
            {
                ["current_dps"] = currentDps,
                ["upgrade_dps"] = upgradeDps,
                ["absolute"] = absolute,
                ["percent"] = percent,
                ["available"] = 1.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating DPS changes");
            return new Dictionary<string, double>
            {
                ["absolute"] = 0.0,
                ["percent"] = 0.0,
                ["available"] = 0.0
            };
        }
    }

    private double CalculateRelativeDps(CharacterStats stats)
    {
        // Use normalized base damage for comparison
        const double baseDamage = 100.0;

        // Apply increased damage modifier
        var damage = baseDamage * (1.0 + stats.IncreasedDamage / 100.0);

        // Apply more damage modifier
        damage *= stats.MoreDamage;

        // Add flat damage
        damage += stats.AddedFlatDamage;

        // Apply crit multiplier (simplified)
        var critMultiplier = 1.0 + (stats.CritChance / 100.0) * (stats.CritMulti / 100.0);
        damage *= critMultiplier;

        return damage;
    }

    private List<string> CheckUpgradeWarnings(
        CharacterStats currentStats,
        CharacterStats upgradeStats,
        Dictionary<string, double> resistanceChanges)
    {
        var warnings = new List<string>();

        // Check for resistance losses
        foreach (var (resType, change) in resistanceChanges)
        {
            if (change < -10)
            {
                warnings.Add($"Loses {Math.Abs(change):F0}% {resType} resistance");
            }
        }

        // Check for life loss
        var lifeChange = upgradeStats.Life - currentStats.Life;
        if (lifeChange < -100)
        {
            warnings.Add($"Loses {Math.Abs(lifeChange):F0} life");
        }

        // Check for ES loss (if using ES)
        if (currentStats.EnergyShield > 500)
        {
            var esChange = upgradeStats.EnergyShield - currentStats.EnergyShield;
            if (esChange < -100)
            {
                warnings.Add($"Loses {Math.Abs(esChange):F0} energy shield");
            }
        }

        // Check for spirit loss
        var spiritChange = upgradeStats.Spirit - currentStats.Spirit;
        if (spiritChange < -10)
        {
            warnings.Add($"Loses {Math.Abs(spiritChange)} spirit");
        }

        return warnings;
    }

    private double CalculatePriorityScore(
        Dictionary<string, Dictionary<string, double>> ehpChanges,
        Dictionary<string, double> dpsChange,
        Dictionary<string, double> resistanceChanges,
        Dictionary<string, double> statChanges,
        CharacterStats currentStats,
        double? priceChaos)
    {
        double score = 50.0; // Base score

        // EHP improvements (weighted heavily)
        var avgEhpPercent = 0.0;
        var ehpCount = 0;
        foreach (var (_, changes) in ehpChanges)
        {
            if (changes.TryGetValue("percent", out var percent))
            {
                avgEhpPercent += percent;
                ehpCount++;
            }
        }
        if (ehpCount > 0)
        {
            avgEhpPercent /= ehpCount;
            score += avgEhpPercent * 0.5; // EHP has high weight
        }

        // DPS improvements
        if (dpsChange.TryGetValue("percent", out var dpsPercent))
        {
            score += dpsPercent * 0.3; // DPS has moderate weight
        }

        // Resistance improvements (critical when below cap)
        foreach (var (resType, change) in resistanceChanges)
        {
            var currentRes = resType switch
            {
                "fire" => currentStats.FireRes,
                "cold" => currentStats.ColdRes,
                "lightning" => currentStats.LightningRes,
                "chaos" => currentStats.ChaosRes,
                _ => 0
            };

            // If below cap and improving, big bonus
            if (currentRes < 75 && change > 0)
            {
                score += change * 0.5;
            }
            // If losing res, penalty
            else if (change < 0)
            {
                score += change * 0.3;
            }
        }

        // Life improvements
        if (statChanges.TryGetValue("life", out var lifeChange))
        {
            score += lifeChange * 0.01; // 1 point per 100 life
        }

        // Spirit improvements
        if (statChanges.TryGetValue("spirit", out var spiritChange))
        {
            score += spiritChange * 0.2; // Spirit is valuable
        }

        // Price penalty (if expensive relative to value)
        if (priceChaos.HasValue && priceChaos.Value > 0)
        {
            var valuePerChaos = score / priceChaos.Value;
            if (valuePerChaos < 1.0)
            {
                score *= 0.8; // Penalty for poor value
            }
        }

        // Clamp score to 0-100
        return Math.Clamp(score, 0, 100);
    }

    private UpgradeRecommendation GenerateRecommendation(
        double priorityScore,
        Dictionary<string, Dictionary<string, double>> ehpChanges,
        Dictionary<string, double> dpsChange,
        List<string> warnings)
    {
        // Strong downgrade
        if (priorityScore < 30)
        {
            return UpgradeRecommendation.Downgrade;
        }

        // Skip (minimal improvement)
        if (priorityScore < 45)
        {
            return UpgradeRecommendation.Skip;
        }

        // Sidegrade (has warnings or mixed changes)
        if (warnings.Count > 0 || priorityScore < 55)
        {
            return UpgradeRecommendation.Sidegrade;
        }

        // Upgrade (moderate improvement)
        if (priorityScore < 70)
        {
            return UpgradeRecommendation.Upgrade;
        }

        // Strong upgrade (significant improvement)
        return UpgradeRecommendation.StrongUpgrade;
    }
}
