using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Content readiness checker.
/// Determines if a character has sufficient defenses for specific endgame content.
/// </summary>
public class ContentReadinessChecker : IContentReadinessChecker
{
    private readonly ILogger<ContentReadinessChecker> _logger;
    private readonly IEhpCalculator _ehpCalculator;
    private readonly Dictionary<ContentDifficulty, DefenseRequirement> _requirements;

    public ContentReadinessChecker(
        ILogger<ContentReadinessChecker> logger,
        IEhpCalculator ehpCalculator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ehpCalculator = ehpCalculator ?? throw new ArgumentNullException(nameof(ehpCalculator));
        _requirements = InitializeRequirements();
        _logger.LogInformation("ContentReadinessChecker initialized");
    }

    public ReadinessReport CheckReadiness(
        DefensiveStats stats,
        ContentDifficulty contentDifficulty,
        double dps = 0)
    {
        ArgumentNullException.ThrowIfNull(stats);

        _logger.LogInformation(
            "Checking readiness for {Content}",
            contentDifficulty);

        var requirements = GetRequirements(contentDifficulty);
        
        // Calculate EHP
        var ehpResults = _ehpCalculator.CalculateEhp(stats);
        var avgEhp = ehpResults.Values.Average();

        // Perform individual checks
        var lifeCheck = CheckLife(stats.Life, requirements);
        var ehpCheck = CheckEhp(avgEhp, requirements);
        var resistanceCheck = CheckResistances(stats, requirements);
        var damageCheck = CheckDamage(dps, requirements);
        var immunityCheck = CheckImmunities(requirements);

        // Collect gaps and warnings
        var gaps = new List<string>();
        var warnings = new List<string>();
        var passes = new List<string>();

        AddLifeMessages(stats.Life, requirements, lifeCheck, gaps, warnings, passes);
        AddEhpMessages(avgEhp, requirements, ehpCheck, gaps, warnings, passes);
        AddResistanceMessages(stats, requirements, resistanceCheck, gaps, warnings, passes);
        AddDamageMessages(dps, requirements, damageCheck, gaps, warnings, passes);

        // Calculate confidence and readiness
        var confidence = CalculateConfidence(lifeCheck, ehpCheck, resistanceCheck, damageCheck);
        var readiness = DetermineReadiness(confidence, gaps.Count, warnings.Count);

        // Generate recommendations
        var recommendations = GenerateRecommendations(gaps, warnings, requirements);
        var priorityUpgrades = GeneratePriorityUpgrades(lifeCheck, ehpCheck, resistanceCheck);

        var report = new ReadinessReport
        {
            ContentName = requirements.ContentName,
            Readiness = readiness,
            Confidence = confidence,
            LifeCheck = lifeCheck,
            EhpCheck = ehpCheck,
            ResistanceCheck = resistanceCheck,
            DamageCheck = damageCheck,
            ImmunityCheck = immunityCheck,
            Gaps = gaps,
            Warnings = warnings,
            Passes = passes,
            Recommendations = recommendations,
            PriorityUpgrades = priorityUpgrades
        };

        _logger.LogInformation(
            "Readiness check complete: {Readiness} (confidence: {Confidence:F1}%)",
            readiness, confidence);

        return report;
    }

    public DefenseRequirement GetRequirements(ContentDifficulty contentDifficulty)
    {
        if (_requirements.TryGetValue(contentDifficulty, out var requirement))
        {
            return requirement;
        }

        throw new ArgumentException(
            $"No requirements defined for {contentDifficulty}",
            nameof(contentDifficulty));
    }

    public IReadOnlyDictionary<ContentDifficulty, DefenseRequirement> GetAllRequirements()
    {
        return _requirements;
    }

    // ============================================================================
    // CHECK METHODS
    // ============================================================================

    private string CheckLife(int life, DefenseRequirement req)
    {
        if (life >= req.RecLife)
            return "pass";
        if (life >= req.MinLife)
            return "warning";
        return "fail";
    }

    private string CheckEhp(double ehp, DefenseRequirement req)
    {
        if (ehp >= req.RecEhp)
            return "pass";
        if (ehp >= req.MinEhp)
            return "warning";
        return "fail";
    }

    private string CheckResistances(DefensiveStats stats, DefenseRequirement req)
    {
        // Check if all required resistances meet minimum
        var fireOk = stats.FireResistance >= req.MinFireRes;
        var coldOk = stats.ColdResistance >= req.MinColdRes;
        var lightningOk = stats.LightningResistance >= req.MinLightningRes;
        var chaosOk = stats.ChaosResistance >= req.MinChaosRes;

        if (!fireOk || !coldOk || !lightningOk || !chaosOk)
            return "fail";

        // Check if all meet recommended
        fireOk = stats.FireResistance >= req.RecFireRes;
        coldOk = stats.ColdResistance >= req.RecColdRes;
        lightningOk = stats.LightningResistance >= req.RecLightningRes;
        chaosOk = stats.ChaosResistance >= req.RecChaosRes;

        if (fireOk && coldOk && lightningOk && chaosOk)
            return "pass";

        return "warning";
    }

    private string CheckDamage(double dps, DefenseRequirement req)
    {
        if (dps >= req.RecDps)
            return "pass";
        if (dps >= req.MinDps || dps == 0) // 0 means not calculated
            return "warning";
        return "fail";
    }

    private string CheckImmunities(DefenseRequirement req)
    {
        // Placeholder - would need actual immunity data from character
        return req.RequiresStunImmunity || req.RequiresFreezeImmunity || req.RequiresCurseImmunity
            ? "warning"
            : "pass";
    }

    // ============================================================================
    // MESSAGE GENERATION
    // ============================================================================

    private void AddLifeMessages(
        int life,
        DefenseRequirement req,
        string check,
        List<string> gaps,
        List<string> warnings,
        List<string> passes)
    {
        switch (check)
        {
            case "pass":
                passes.Add($"Life: {life} (meets recommended {req.RecLife})");
                break;
            case "warning":
                var needed = req.RecLife - life;
                warnings.Add($"Life: {life} (recommended: {req.RecLife}, need +{needed})");
                break;
            case "fail":
                var shortfall = req.MinLife - life;
                gaps.Add($"Life: {life} (CRITICAL: minimum {req.MinLife}, need +{shortfall})");
                break;
        }
    }

    private void AddEhpMessages(
        double ehp,
        DefenseRequirement req,
        string check,
        List<string> gaps,
        List<string> warnings,
        List<string> passes)
    {
        switch (check)
        {
            case "pass":
                passes.Add($"EHP: {ehp:F0} (meets recommended {req.RecEhp})");
                break;
            case "warning":
                var needed = req.RecEhp - (int)ehp;
                warnings.Add($"EHP: {ehp:F0} (recommended: {req.RecEhp}, need +{needed})");
                break;
            case "fail":
                var shortfall = req.MinEhp - (int)ehp;
                gaps.Add($"EHP: {ehp:F0} (CRITICAL: minimum {req.MinEhp}, need +{shortfall})");
                break;
        }
    }

    private void AddResistanceMessages(
        DefensiveStats stats,
        DefenseRequirement req,
        string check,
        List<string> gaps,
        List<string> warnings,
        List<string> passes)
    {
        if (check == "fail")
        {
            if (stats.FireResistance < req.MinFireRes)
                gaps.Add($"Fire Res: {stats.FireResistance}% (min: {req.MinFireRes}%)");
            if (stats.ColdResistance < req.MinColdRes)
                gaps.Add($"Cold Res: {stats.ColdResistance}% (min: {req.MinColdRes}%)");
            if (stats.LightningResistance < req.MinLightningRes)
                gaps.Add($"Lightning Res: {stats.LightningResistance}% (min: {req.MinLightningRes}%)");
            if (stats.ChaosResistance < req.MinChaosRes)
                gaps.Add($"Chaos Res: {stats.ChaosResistance}% (min: {req.MinChaosRes}%)");
        }
        else if (check == "warning")
        {
            if (stats.FireResistance < req.RecFireRes)
                warnings.Add($"Fire Res: {stats.FireResistance}% (rec: {req.RecFireRes}%)");
            if (stats.ColdResistance < req.RecColdRes)
                warnings.Add($"Cold Res: {stats.ColdResistance}% (rec: {req.RecColdRes}%)");
            if (stats.LightningResistance < req.RecLightningRes)
                warnings.Add($"Lightning Res: {stats.LightningResistance}% (rec: {req.RecLightningRes}%)");
            if (stats.ChaosResistance < req.RecChaosRes)
                warnings.Add($"Chaos Res: {stats.ChaosResistance}% (rec: {req.RecChaosRes}%)");
        }
        else
        {
            passes.Add("All resistances meet requirements");
        }
    }

    private void AddDamageMessages(
        double dps,
        DefenseRequirement req,
        string check,
        List<string> gaps,
        List<string> warnings,
        List<string> passes)
    {
        if (dps == 0)
        {
            warnings.Add("DPS not calculated");
            return;
        }

        switch (check)
        {
            case "pass":
                passes.Add($"DPS: {dps:F0} (meets recommended {req.RecDps})");
                break;
            case "warning":
                warnings.Add($"DPS: {dps:F0} (recommended: {req.RecDps})");
                break;
            case "fail":
                gaps.Add($"DPS: {dps:F0} (CRITICAL: minimum {req.MinDps})");
                break;
        }
    }

    // ============================================================================
    // ANALYSIS METHODS
    // ============================================================================

    private double CalculateConfidence(
        string lifeCheck,
        string ehpCheck,
        string resistanceCheck,
        string damageCheck)
    {
        var score = 0.0;

        // Life (30% of confidence)
        score += lifeCheck switch
        {
            "pass" => 30.0,
            "warning" => 20.0,
            _ => 0.0
        };

        // EHP (30% of confidence)
        score += ehpCheck switch
        {
            "pass" => 30.0,
            "warning" => 20.0,
            _ => 0.0
        };

        // Resistances (30% of confidence)
        score += resistanceCheck switch
        {
            "pass" => 30.0,
            "warning" => 15.0,
            _ => 0.0
        };

        // Damage (10% of confidence)
        score += damageCheck switch
        {
            "pass" => 10.0,
            "warning" => 5.0,
            _ => 0.0
        };

        return score;
    }

    private ReadinessLevel DetermineReadiness(
        double confidence,
        int gapCount,
        int warningCount)
    {
        if (gapCount > 0)
            return ReadinessLevel.NotReady;
        
        if (confidence >= 80)
            return ReadinessLevel.Ready;
        
        if (confidence >= 60)
            return ReadinessLevel.MostlyReady;
        
        return ReadinessLevel.Risky;
    }

    private List<string> GenerateRecommendations(
        List<string> gaps,
        List<string> warnings,
        DefenseRequirement req)
    {
        var recommendations = new List<string>();

        if (gaps.Count > 0)
        {
            recommendations.Add("CRITICAL gaps found - address these before attempting content");
        }

        if (warnings.Count > 0)
        {
            recommendations.Add("Some stats below recommended - proceed with caution");
        }

        // Add content-specific tips
        recommendations.AddRange(req.Tips);

        return recommendations;
    }

    private List<string> GeneratePriorityUpgrades(
        string lifeCheck,
        string ehpCheck,
        string resistanceCheck)
    {
        var upgrades = new List<string>();

        if (lifeCheck == "fail")
            upgrades.Add("PRIORITY: Increase life pool immediately");
        else if (lifeCheck == "warning")
            upgrades.Add("Improve life pool for better survivability");

        if (resistanceCheck == "fail")
            upgrades.Add("PRIORITY: Cap elemental resistances (75%)");
        else if (resistanceCheck == "warning")
            upgrades.Add("Consider overcapping resistances");

        if (ehpCheck == "fail")
            upgrades.Add("PRIORITY: Add defensive layers (armor/evasion/ES)");
        else if (ehpCheck == "warning")
            upgrades.Add("Strengthen defensive layers");

        return upgrades;
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private Dictionary<ContentDifficulty, DefenseRequirement> InitializeRequirements()
    {
        var reqs = new Dictionary<ContentDifficulty, DefenseRequirement>();

        // Campaign
        reqs[ContentDifficulty.Campaign] = new DefenseRequirement
        {
            ContentName = "Campaign",
            Difficulty = ContentDifficulty.Campaign,
            MinLife = 800,
            MinEhp = 800,
            MinFireRes = 0,
            MinColdRes = 0,
            MinLightningRes = 0,
            MinChaosRes = -60,
            RecLife = 1500,
            RecEhp = 1500,
            RecFireRes = 50,
            RecColdRes = 50,
            RecLightningRes = 50,
            RecChaosRes = -30,
            RecDps = 5000,
            MinDps = 2000,
            Tips = new List<string>
            {
                "Focus on life and resistances",
                "Damage isn't critical yet",
                "Learn boss mechanics"
            }
        };

        // Early Maps (T1-T5)
        reqs[ContentDifficulty.EarlyMaps] = new DefenseRequirement
        {
            ContentName = "Early Maps (T1-T5)",
            Difficulty = ContentDifficulty.EarlyMaps,
            MinLife = 2000,
            MinEhp = 3000,
            MinFireRes = 60,
            MinColdRes = 60,
            MinLightningRes = 60,
            MinChaosRes = -50,
            RecLife = 3000,
            RecEhp = 5000,
            RecFireRes = 75,
            RecColdRes = 75,
            RecLightningRes = 75,
            RecChaosRes = -20,
            RecDps = 20000,
            MinDps = 10000,
            DangerousMechanics = new List<string> { "Rare monster mods", "Maven invitations" },
            Tips = new List<string>
            {
                "Cap elemental resistances (75%)",
                "3000+ life is comfortable",
                "Focus on clearing speed"
            }
        };

        // Mid Maps (T6-T10)
        reqs[ContentDifficulty.MidMaps] = new DefenseRequirement
        {
            ContentName = "Mid Maps (T6-T10)",
            Difficulty = ContentDifficulty.MidMaps,
            MinLife = 3000,
            MinEhp = 5000,
            MinFireRes = 75,
            MinColdRes = 75,
            MinLightningRes = 75,
            MinChaosRes = -30,
            RecLife = 4000,
            RecEhp = 7000,
            RecFireRes = 75,
            RecColdRes = 75,
            RecLightningRes = 75,
            RecChaosRes = 0,
            RecDps = 50000,
            MinDps = 25000,
            MinPhysMitigation = 20.0,
            DangerousMechanics = new List<string> { "Essence mobs", "Expedition", "Breach" },
            Tips = new List<string>
            {
                "Elemental resistances MUST be capped",
                "Start investing in chaos res",
                "Add defensive layers (armor, evasion, block)"
            }
        };

        // High Maps (T11-T15)
        reqs[ContentDifficulty.HighMaps] = new DefenseRequirement
        {
            ContentName = "High Maps (T11-T15)",
            Difficulty = ContentDifficulty.HighMaps,
            MinLife = 4000,
            MinEhp = 8000,
            MinFireRes = 75,
            MinColdRes = 75,
            MinLightningRes = 75,
            MinChaosRes = -10,
            RecLife = 5000,
            RecEhp = 12000,
            RecFireRes = 85,
            RecColdRes = 85,
            RecLightningRes = 85,
            RecChaosRes = 20,
            RecDps = 100000,
            MinDps = 50000,
            MinPhysMitigation = 30.0,
            MinEleMitigation = 10.0,
            DangerousMechanics = new List<string> { "Delirium", "Simulacrum", "Uber Expedition" },
            Tips = new List<string>
            {
                "5000+ life strongly recommended",
                "Overcap resistances for map mods",
                "Multiple defense layers required",
                "DPS becomes important for safety"
            }
        };

        // Pinnacle Maps (T16+)
        reqs[ContentDifficulty.PinnacleMaps] = new DefenseRequirement
        {
            ContentName = "Pinnacle Maps (T16+)",
            Difficulty = ContentDifficulty.PinnacleMaps,
            MinLife = 5000,
            MinEhp = 12000,
            MinFireRes = 75,
            MinColdRes = 75,
            MinLightningRes = 75,
            MinChaosRes = 0,
            RecLife = 6500,
            RecEhp = 15000,
            RecFireRes = 90,
            RecColdRes = 90,
            RecLightningRes = 90,
            RecChaosRes = 40,
            RecDps = 200000,
            MinDps = 100000,
            MinPhysMitigation = 40.0,
            MinEleMitigation = 20.0,
            DangerousMechanics = new List<string> { "All map mods", "Multiple damage types", "High DPS checks" },
            Tips = new List<string>
            {
                "6000+ life is baseline",
                "Heavily overcap resistances",
                "Need 3+ defensive layers",
                "High DPS = better defense (kill before they kill you)",
                "Consider corrupted blood immunity"
            }
        };

        // Boss Normal
        reqs[ContentDifficulty.BossNormal] = new DefenseRequirement
        {
            ContentName = "Normal Endgame Bosses",
            Difficulty = ContentDifficulty.BossNormal,
            MinLife = 4000,
            MinEhp = 8000,
            MinFireRes = 75,
            MinColdRes = 75,
            MinLightningRes = 75,
            MinChaosRes = -10,
            RecLife = 5500,
            RecEhp = 10000,
            RecFireRes = 75,
            RecColdRes = 75,
            RecLightningRes = 75,
            RecChaosRes = 20,
            RecDps = 75000,
            MinDps = 40000,
            DangerousMechanics = new List<string> { "Boss-specific attacks", "Telegraphed abilities" },
            Tips = new List<string>
            {
                "Learn mechanics > raw tankiness",
                "5000+ life recommended",
                "Capped resistances mandatory",
                "Practice dodge timing"
            }
        };

        // Boss Pinnacle
        reqs[ContentDifficulty.BossPinnacle] = new DefenseRequirement
        {
            ContentName = "Pinnacle Bosses",
            Difficulty = ContentDifficulty.BossPinnacle,
            MinLife = 5500,
            MinEhp = 12000,
            MinFireRes = 75,
            MinColdRes = 75,
            MinLightningRes = 75,
            MinChaosRes = 20,
            RecLife = 7000,
            RecEhp = 18000,
            RecFireRes = 75,
            RecColdRes = 75,
            RecLightningRes = 75,
            RecChaosRes = 50,
            RecDps = 300000,
            MinDps = 150000,
            RequiresStunImmunity = true,
            DangerousMechanics = new List<string> { "One-shot mechanics", "Stacking debuffs", "Enrage timers" },
            Tips = new List<string>
            {
                "7000+ life is minimum",
                "All resistances capped",
                "Stun immunity highly recommended",
                "Very high DPS required (enrage timers)",
                "Master the fight mechanics"
            }
        };

        return reqs;
    }
}
