using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Build scoring and analysis engine.
/// Evaluates build quality on a 0.0-1.0 scale and assigns tier rankings.
/// </summary>
public class BuildScorer : IBuildScorer
{
    private readonly ILogger<BuildScorer> _logger;
    private readonly IEhpCalculator _ehpCalculator;
    private readonly IWeaknessDetector _weaknessDetector;

    // Scoring thresholds
    private const int EXCELLENT_LIFE = 6000;
    private const int GOOD_LIFE = 4500;
    private const int MINIMUM_LIFE = 3000;
    private const int EXCELLENT_RES = 75;
    private const int GOOD_RES = 60;
    private const double GEAR_WEIGHT = 0.4;
    private const double PASSIVE_WEIGHT = 0.4;
    private const double SKILL_WEIGHT = 0.2;

    public BuildScorer(
        ILogger<BuildScorer> _logger,
        IEhpCalculator ehpCalculator,
        IWeaknessDetector weaknessDetector)
    {
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        _ehpCalculator = ehpCalculator ?? throw new ArgumentNullException(nameof(ehpCalculator));
        _weaknessDetector = weaknessDetector ?? throw new ArgumentNullException(nameof(weaknessDetector));
        _logger.LogInformation("BuildScorer initialized");
    }

    public BuildScore AnalyzeBuild(CharacterData characterData)
    {
        ArgumentNullException.ThrowIfNull(characterData);

        _logger.LogInformation("Analyzing build for character: {Name}", characterData.Name);

        try
        {
            // Calculate component scores
            var gearScore = ScoreGear(characterData);
            var passiveScore = ScorePassiveTree(characterData.Passives.Count, characterData.Level);
            var skillScore = ScoreSkills(characterData);

            // Calculate weighted overall score
            var overallScore = (gearScore * GEAR_WEIGHT) +
                             (passiveScore * PASSIVE_WEIGHT) +
                             (skillScore * SKILL_WEIGHT);

            // Determine tier
            var tier = DetermineTier(overallScore);

            // Calculate defense rating
            var defenseRating = CalculateDefenseRating(characterData.Stats);

            // Calculate average EHP
            var ehp = CalculateAverageEhp(characterData.Stats);

            // Identify strengths and weaknesses using the weakness detector
            var weaknessResult = _weaknessDetector.DetectWeaknesses(characterData.Stats);
            var strengths = IdentifyStrengths(gearScore, passiveScore, skillScore, defenseRating);
            var weaknesses = weaknessResult.Weaknesses
                .Where(w => w.Severity is WeaknessSeverity.Critical or WeaknessSeverity.High)
                .Select(w => w.Title)
                .ToList();

            var result = new BuildScore
            {
                OverallScore = overallScore,
                Tier = tier,
                Strengths = strengths,
                Weaknesses = weaknesses,
                GearScore = gearScore,
                PassiveScore = passiveScore,
                SkillScore = skillScore,
                Dps = 0, // TODO: Calculate when damage calculator integration is complete
                Ehp = ehp,
                DefenseRating = defenseRating
            };

            _logger.LogInformation(
                "Build analysis complete: Tier {Tier}, Score {Score:F2}",
                tier, overallScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build analysis failed");
            throw;
        }
    }

    public double ScoreGear(CharacterData characterData)
    {
        ArgumentNullException.ThrowIfNull(characterData);

        double score = 0.5; // Base score

        var stats = characterData.Stats;

        // Life score
        if (stats.Life >= EXCELLENT_LIFE)
            score += 0.15;
        else if (stats.Life >= GOOD_LIFE)
            score += 0.1;
        else if (stats.Life >= MINIMUM_LIFE)
            score += 0.05;

        // Resistance score
        var avgEleRes = (stats.FireResistance + stats.ColdResistance + stats.LightningResistance) / 3.0;
        if (avgEleRes >= EXCELLENT_RES)
            score += 0.15;
        else if (avgEleRes >= GOOD_RES)
            score += 0.1;
        else if (avgEleRes >= 50)
            score += 0.05;

        // Armor/Evasion/ES score
        var hasDefense = stats.Armor >= 5000 || stats.Evasion >= 5000 || stats.EnergyShield >= 1000;
        if (hasDefense)
            score += 0.1;

        // Spirit score (bonus for having spirit gear)
        if (stats.Spirit >= 100)
            score += 0.1;
        else if (stats.Spirit >= 50)
            score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public double ScorePassiveTree(int passiveCount, int level)
    {
        if (level <= 0)
            throw new ArgumentException("Level must be positive", nameof(level));

        // Expected passives based on level
        var expectedPassives = CalculateExpectedPassives(level);

        // Score based on how close to expected
        var ratio = (double)passiveCount / expectedPassives;

        double score;
        if (ratio >= 0.95 && ratio <= 1.05)
        {
            // Near perfect allocation
            score = 1.0;
        }
        else if (ratio >= 0.85 && ratio <= 1.15)
        {
            // Good allocation
            score = 0.8;
        }
        else if (ratio >= 0.7 && ratio <= 1.3)
        {
            // Acceptable allocation
            score = 0.6;
        }
        else if (ratio >= 0.5)
        {
            // Under-allocated or heavily over-allocated
            score = 0.4;
        }
        else
        {
            // Severely under-allocated
            score = 0.2;
        }

        return score;
    }

    public double ScoreSkills(CharacterData characterData)
    {
        ArgumentNullException.ThrowIfNull(characterData);

        double score = 0.5; // Base score

        var skillCount = characterData.Skills.Count;

        // Penalize for too few or too many skills
        if (skillCount >= 4 && skillCount <= 6)
        {
            score += 0.3; // Optimal number of skills
        }
        else if (skillCount >= 2 && skillCount <= 8)
        {
            score += 0.2; // Acceptable number
        }
        else if (skillCount >= 1)
        {
            score += 0.1; // At least has skills
        }

        // Bonus for having support gems (indicates thought-out skill setup)
        var skillsWithSupports = characterData.Skills.Count(s => 
            s.Gems.Any(g => g.IsSupport));
        if (skillsWithSupports >= 3)
            score += 0.2;
        else if (skillsWithSupports >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public double CalculateDefenseRating(DefensiveStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        double rating = 0.0;

        // Life pool rating (0-0.3)
        if (stats.Life >= EXCELLENT_LIFE)
            rating += 0.3;
        else if (stats.Life >= GOOD_LIFE)
            rating += 0.2;
        else if (stats.Life >= MINIMUM_LIFE)
            rating += 0.1;

        // Resistance rating (0-0.4)
        var avgEleRes = (stats.FireResistance + stats.ColdResistance + stats.LightningResistance) / 3.0;
        if (avgEleRes >= EXCELLENT_RES)
            rating += 0.4;
        else if (avgEleRes >= GOOD_RES)
            rating += 0.3;
        else if (avgEleRes >= 50)
            rating += 0.2;
        else if (avgEleRes >= 25)
            rating += 0.1;

        // Defense layers rating (0-0.3)
        var layerCount = 0;
        if (stats.Armor >= 5000) layerCount++;
        if (stats.Evasion >= 5000) layerCount++;
        if (stats.EnergyShield >= 1000) layerCount++;
        if (stats.BlockChance >= 50) layerCount++;

        rating += layerCount * 0.075; // Up to 0.3 for 4 layers

        return Math.Clamp(rating, 0.0, 1.0);
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private BuildTier DetermineTier(double score)
    {
        return score switch
        {
            >= 0.9 => BuildTier.S,
            >= 0.75 => BuildTier.A,
            >= 0.6 => BuildTier.B,
            >= 0.4 => BuildTier.C,
            >= 0.2 => BuildTier.D,
            _ => BuildTier.F
        };
    }

    private double CalculateAverageEhp(DefensiveStats stats)
    {
        var ehpResults = _ehpCalculator.CalculateEhp(stats);
        return ehpResults.Values.Average();
    }

    private List<string> IdentifyStrengths(
        double gearScore,
        double passiveScore,
        double skillScore,
        double defenseRating)
    {
        var strengths = new List<string>();

        if (gearScore >= 0.8)
            strengths.Add("Well-geared character");
        if (passiveScore >= 0.8)
            strengths.Add("Efficient passive tree");
        if (skillScore >= 0.8)
            strengths.Add("Optimized skill setup");
        if (defenseRating >= 0.8)
            strengths.Add("Strong defensive layers");
        if (defenseRating >= 0.6 && gearScore >= 0.7)
            strengths.Add("Balanced offense and defense");

        return strengths;
    }

    private int CalculateExpectedPassives(int level)
    {
        // PoE2 passive allocation: Start with some passives, gain 1 per level
        // Estimate based on typical progression
        const int BASE_PASSIVES = 8; // Starting passives
        const int PASSIVES_PER_LEVEL = 1;
        
        return BASE_PASSIVES + (level * PASSIVES_PER_LEVEL);
    }
}
