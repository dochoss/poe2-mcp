using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Models;
using System.Text;
using System.Text.Json;

namespace Poe2Mcp.Core.Optimizers;

/// <summary>
/// Calculate optimal spell gem + support gem combinations considering DPS, spirit cost, and utility
/// </summary>
public class GemSynergyCalculator : IGemSynergyCalculator
{
    private readonly Poe2DbContext _dbContext;
    private readonly ILogger<GemSynergyCalculator> _logger;

    public GemSynergyCalculator(Poe2DbContext dbContext, ILogger<GemSynergyCalculator> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SynergyResult>> FindBestCombinationsAsync(
        string spellName,
        CharacterModifiers? characterMods = null,
        int maxSpirit = 100,
        int numSupports = 5,
        OptimizationGoal goal = OptimizationGoal.Dps,
        int topN = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spellName))
        {
            throw new ArgumentException("Spell name cannot be null or empty", nameof(spellName));
        }

        characterMods ??= new CharacterModifiers();

        _logger.LogInformation(
            "Finding best {NumSupports}-support combinations for {SpellName} (goal={Goal}, maxSpirit={MaxSpirit})",
            numSupports, spellName, goal, maxSpirit);

        // Get spell gem from database
        var spell = await GetSpellGemAsync(spellName, cancellationToken);
        if (spell == null)
        {
            _logger.LogWarning("Spell '{SpellName}' not found in database", spellName);
            return Array.Empty<SynergyResult>();
        }

        // Get compatible support gems
        var compatibleSupports = await GetCompatibleSupportsAsync(spell, cancellationToken);

        if (compatibleSupports.Count < numSupports)
        {
            _logger.LogWarning(
                "Only {Count} compatible supports found (need {NumSupports})",
                compatibleSupports.Count, numSupports);
            numSupports = compatibleSupports.Count;
        }

        _logger.LogInformation("Found {Count} compatible support gems", compatibleSupports.Count);

        // Generate and evaluate all combinations
        var results = await GenerateAndEvaluateCombinationsAsync(
            spell,
            compatibleSupports,
            numSupports,
            characterMods,
            maxSpirit,
            goal,
            cancellationToken);

        // Sort by overall score and return top N
        var topResults = results
            .OrderByDescending(r => r.OverallScore)
            .Take(topN)
            .ToList();

        _logger.LogInformation("Calculated {Total} valid combinations, returning top {TopN}", results.Count, topResults.Count);

        return topResults;
    }

    /// <inheritdoc/>
    public string FormatResult(SynergyResult result, bool detailed = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"Spell: {result.SpellName}");
        sb.AppendLine($"Support Gems: {string.Join(", ", result.SupportGems)}");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"Total DPS: {result.TotalDps:N1}");
        sb.AppendLine($"Average Hit: {result.AverageHit:N1}");
        sb.AppendLine($"Casts per Second: {result.CastsPerSecond:F2}");
        sb.AppendLine($"Spirit Cost: {result.TotalSpiritCost}");
        sb.AppendLine($"Mana Cost: {result.TotalManaCost:F1}");
        sb.AppendLine($"More Multiplier: {result.TotalMoreMultiplier:F2}x");
        sb.AppendLine($"Increased Damage: {result.TotalIncreasedDamage:+F0}%");

        if (result.UtilityEffects.Count > 0)
        {
            sb.AppendLine($"Utility: {string.Join(", ", result.UtilityEffects)}");
        }

        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"DPS Score: {result.DpsScore:N1}");
        sb.AppendLine($"Efficiency (DPS/Spirit): {result.EfficiencyScore:N1}");
        sb.AppendLine($"Overall Score: {result.OverallScore:N1}");

        if (detailed && result.CalculationBreakdown.Count > 0)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("Calculation Breakdown:");
            foreach (var kvp in result.CalculationBreakdown)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        sb.AppendLine(new string('=', 80));

        return sb.ToString();
    }

    private async Task<GemStats?> GetSpellGemAsync(string spellName, CancellationToken cancellationToken)
    {
        var normalized = spellName.ToLowerInvariant();

        var skillGem = await _dbContext.SkillGems
            .FirstOrDefaultAsync(s => s.Name.ToLower() == normalized, cancellationToken);

        if (skillGem == null)
        {
            return null;
        }

        // Parse tags
        var tags = ParseJsonStringList(skillGem.Tags);

        // Parse base damage (assumed to be stored as JSON with min/max)
        var baseDamage = ParseBaseDamage(skillGem.BaseDamage);

        return new GemStats
        {
            Name = skillGem.Name,
            Tags = tags,
            BaseDamageMin = baseDamage.Min,
            BaseDamageMax = baseDamage.Max,
            CastTime = skillGem.AttackSpeed ?? 1.0,
            CritChance = skillGem.CritChance ?? 0.0,
            DamageEffectiveness = skillGem.DamageEffectiveness ?? 100.0,
            ManaCost = skillGem.ManaCost ?? 0.0,
            SpiritCost = 0 // Base spell spirit cost (usually 0)
        };
    }

    private async Task<List<SupportGemEffect>> GetCompatibleSupportsAsync(
        GemStats spell,
        CancellationToken cancellationToken)
    {
        var allSupports = await _dbContext.SupportGems.ToListAsync(cancellationToken);
        var compatible = new List<SupportGemEffect>();

        foreach (var support in allSupports)
        {
            // Parse compatible tags
            var compatibleTags = ParseJsonStringList(support.CompatibleTags);

            // Check if support is compatible with spell
            if (compatibleTags.Count > 0)
            {
                var hasMatch = spell.Tags.Any(spellTag =>
                    compatibleTags.Any(compatTag =>
                        compatTag.Equals(spellTag, StringComparison.OrdinalIgnoreCase)));

                if (!hasMatch)
                {
                    continue;
                }
            }

            // Parse modifiers
            var modifiers = ParseModifiers(support.Modifiers);

            compatible.Add(new SupportGemEffect
            {
                Name = support.Name,
                Tags = ParseJsonStringList(support.Tags),
                MoreDamage = modifiers.GetValueOrDefault("more_damage", 0.0),
                MoreCastSpeed = modifiers.GetValueOrDefault("more_cast_speed", 0.0),
                MoreAoe = modifiers.GetValueOrDefault("more_aoe", 0.0),
                MoreCritChance = modifiers.GetValueOrDefault("more_crit_chance", 0.0),
                MoreCritDamage = modifiers.GetValueOrDefault("more_crit_damage", 0.0),
                LessDamage = modifiers.GetValueOrDefault("less_damage", 0.0),
                LessCastSpeed = modifiers.GetValueOrDefault("less_cast_speed", 0.0),
                IncreasedDamage = modifiers.GetValueOrDefault("increased_damage", 0.0),
                IncreasedCastSpeed = modifiers.GetValueOrDefault("increased_cast_speed", 0.0),
                IncreasedCritChance = modifiers.GetValueOrDefault("increased_crit_chance", 0.0),
                SpiritCost = ParseSpiritCost(support),
                ManaCostMultiplier = support.ManaMultiplier ?? 100.0,
                RequiredTags = compatibleTags
            });
        }

        return compatible;
    }

    private async Task<List<SynergyResult>> GenerateAndEvaluateCombinationsAsync(
        GemStats spell,
        List<SupportGemEffect> supports,
        int numSupports,
        CharacterModifiers characterMods,
        int maxSpirit,
        OptimizationGoal goal,
        CancellationToken cancellationToken)
    {
        var results = new List<SynergyResult>();

        // Generate combinations
        var combinations = GetCombinations(supports, numSupports);
        var totalCombinations = combinations.Count();

        _logger.LogInformation("Testing {Total} combinations...", totalCombinations);

        foreach (var combo in combinations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate DPS and metrics
            var result = CalculateCombinationDps(spell, combo.ToList(), characterMods, maxSpirit);

            if (result != null)
            {
                // Calculate scores based on goal
                ScoreResult(result, goal);
                results.Add(result);
            }
        }

        return results;
    }

    private SynergyResult? CalculateCombinationDps(
        GemStats spell,
        List<SupportGemEffect> supports,
        CharacterModifiers characterMods,
        int maxSpirit)
    {
        // Calculate total spirit cost
        var totalSpirit = spell.SpiritCost + supports.Sum(s => s.SpiritCost);

        // Skip if over spirit budget
        if (totalSpirit > maxSpirit)
        {
            return null;
        }

        // Start with base damage
        var baseDamageAvg = (spell.BaseDamageMin + spell.BaseDamageMax) / 2;

        if (baseDamageAvg == 0)
        {
            return null; // No base damage data
        }

        // Accumulate modifiers
        double totalMoreDamage = characterMods.MoreDamage;
        double totalIncreasedDamage = characterMods.IncreasedDamage;
        double totalMoreCastSpeed = characterMods.MoreCastSpeed;
        double totalIncreasedCastSpeed = characterMods.IncreasedCastSpeed;
        double totalAddedDamage = 0.0;
        var utilityEffects = new List<string>();
        double totalManaCost = spell.ManaCost;

        // Apply each support's effects
        foreach (var support in supports)
        {
            // More multipliers (multiplicative)
            if (support.MoreDamage != 0)
            {
                totalMoreDamage *= (1.0 + support.MoreDamage / 100.0);
            }

            if (support.MoreCastSpeed != 0)
            {
                totalMoreCastSpeed *= (1.0 + support.MoreCastSpeed / 100.0);
            }

            // Less multipliers (multiplicative penalties)
            if (support.LessDamage != 0)
            {
                totalMoreDamage *= (1.0 - support.LessDamage / 100.0);
            }

            if (support.LessCastSpeed != 0)
            {
                totalMoreCastSpeed *= (1.0 - support.LessCastSpeed / 100.0);
            }

            // Increased (additive)
            totalIncreasedDamage += support.IncreasedDamage;
            totalIncreasedCastSpeed += support.IncreasedCastSpeed;

            // Added damage
            if (support.AddedDamageMin > 0 || support.AddedDamageMax > 0)
            {
                var addedAvg = (support.AddedDamageMin + support.AddedDamageMax) / 2;
                totalAddedDamage += addedAvg * (spell.DamageEffectiveness / 100.0);
            }

            // Utility
            utilityEffects.AddRange(support.UtilityEffects);

            // Mana cost
            totalManaCost *= (support.ManaCostMultiplier / 100.0);
        }

        // Calculate final damage
        var damageAfterAdded = baseDamageAvg + totalAddedDamage;
        var damageAfterIncreased = damageAfterAdded * (1.0 + totalIncreasedDamage / 100.0);
        var finalDamage = damageAfterIncreased * totalMoreDamage;

        // Calculate cast speed
        var baseCastsPerSec = 1.0 / spell.CastTime;
        var castsAfterIncreased = baseCastsPerSec * (1.0 + totalIncreasedCastSpeed / 100.0);
        var finalCastsPerSec = castsAfterIncreased * totalMoreCastSpeed;

        // Calculate DPS
        var totalDps = finalDamage * finalCastsPerSec;

        // Create result
        return new SynergyResult
        {
            SpellName = spell.Name,
            SupportGems = supports.Select(s => s.Name).ToList(),
            TotalDps = totalDps,
            AverageHit = finalDamage,
            CastsPerSecond = finalCastsPerSec,
            TotalSpiritCost = totalSpirit,
            TotalManaCost = totalManaCost,
            TotalMoreMultiplier = totalMoreDamage,
            TotalIncreasedDamage = totalIncreasedDamage,
            UtilityEffects = utilityEffects,
            CalculationBreakdown = new Dictionary<string, object>
            {
                ["base_damage"] = baseDamageAvg,
                ["added_damage"] = totalAddedDamage,
                ["after_increased"] = damageAfterIncreased,
                ["after_more"] = finalDamage,
                ["more_multiplier"] = totalMoreDamage,
                ["increased_total"] = totalIncreasedDamage,
                ["cast_speed_multiplier"] = totalMoreCastSpeed,
                ["spirit_per_support"] = supports.Select(s => s.SpiritCost).ToArray()
            }
        };
    }

    private void ScoreResult(SynergyResult result, OptimizationGoal goal)
    {
        // DPS score (normalized)
        result.DpsScore = result.TotalDps;

        // Efficiency score (DPS per spirit)
        if (result.TotalSpiritCost > 0)
        {
            result.EfficiencyScore = result.TotalDps / result.TotalSpiritCost;
        }
        else
        {
            result.EfficiencyScore = result.TotalDps;
        }

        // Utility score (bonus for utility effects)
        var utilityScore = result.UtilityEffects.Count * 1000; // Arbitrary bonus

        // Calculate overall score based on goal
        result.OverallScore = goal switch
        {
            OptimizationGoal.Dps => result.DpsScore,
            OptimizationGoal.Efficiency => result.EfficiencyScore,
            OptimizationGoal.Utility => result.DpsScore * 0.7 + utilityScore,
            _ => result.DpsScore * 0.6 + result.EfficiencyScore * 10.0 + utilityScore * 0.1 // Balanced
        };
    }

    private List<string> ParseJsonStringList(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private (double Min, double Max) ParseBaseDamage(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return (0, 0);
        }

        try
        {
            var damage = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
            if (damage != null)
            {
                return (
                    damage.GetValueOrDefault("min", 0),
                    damage.GetValueOrDefault("max", 0)
                );
            }
        }
        catch { }

        return (0, 0);
    }

    private Dictionary<string, double> ParseModifiers(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, double>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(json)
                ?? new Dictionary<string, double>();
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    private int ParseSpiritCost(Data.Models.SupportGem support)
    {
        // Spirit cost might be in modifiers or a separate field
        // For now, return a default value based on gem level
        var level = support.RequiredLevel ?? 1;
        return level / 10; // Simple heuristic
    }

    private IEnumerable<IEnumerable<T>> GetCombinations<T>(List<T> list, int length)
    {
        if (length == 0)
        {
            return new[] { Array.Empty<T>() };
        }

        return list.SelectMany((item, index) =>
            GetCombinations(list.Skip(index + 1).ToList(), length - 1)
                .Select(combination => new[] { item }.Concat(combination)));
    }
}
