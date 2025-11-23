using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Analyzers;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Analyzers;

public class BuildScorerTests
{
    private readonly IBuildScorer _scorer;

    public BuildScorerTests()
    {
        var ehpCalculator = new EhpCalculator();
        var weaknessDetector = new WeaknessDetector(NullLogger<WeaknessDetector>.Instance);
        _scorer = new BuildScorer(
            NullLogger<BuildScorer>.Instance,
            ehpCalculator,
            weaknessDetector);
    }

    [Fact]
    public void AnalyzeBuild_WithExcellentBuild_ReturnsHighScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "TestChar",
            Class = "Warrior",
            Level = 80,
            Stats = new DefensiveStats
            {
                Life = 6500,
                EnergyShield = 0,
                Armor = 15000,
                FireResistance = 75,
                ColdResistance = 75,
                LightningResistance = 75,
                ChaosResistance = 50,
                Spirit = 120
            },
            Passives = Enumerable.Range(1, 88).ToList(), // 88 passives at level 80
            Skills = new List<SkillData>
            {
                new() { Name = "Main Attack", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Movement", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Aura 1", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Aura 2", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Guard Skill", Gems = new() { new() { IsSupport = true } } }
            }
        };

        // Act
        var result = _scorer.AnalyzeBuild(character);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OverallScore >= 0.7, "Excellent build should score >= 0.7");
        Assert.True(
            result.Tier is BuildTier.S or BuildTier.A or BuildTier.B,
            "Excellent build should be tier S, A, or B");
        Assert.True(result.Strengths.Count > 0, "Should have identified strengths");
    }

    [Fact]
    public void AnalyzeBuild_WithPoorBuild_ReturnsLowScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "PoorChar",
            Class = "Witch",
            Level = 70,
            Stats = new DefensiveStats
            {
                Life = 2000,
                EnergyShield = 0,
                Armor = 500,
                FireResistance = 20,
                ColdResistance = 15,
                LightningResistance = 25,
                ChaosResistance = -60,
                Spirit = 10
            },
            Passives = Enumerable.Range(1, 30).ToList(), // Under-allocated
            Skills = new List<SkillData>
            {
                new() { Name = "Main Attack", Gems = new() }
            }
        };

        // Act
        var result = _scorer.AnalyzeBuild(character);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OverallScore < 0.6, "Poor build should score < 0.6");
        Assert.True(
            result.Tier is BuildTier.C or BuildTier.D or BuildTier.F,
            "Poor build should be tier C, D, or F");
        Assert.True(result.Weaknesses.Count > 0, "Should have identified weaknesses");
    }

    [Fact]
    public void ScoreGear_WithExcellentGear_ReturnsHighScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "TestChar",
            Stats = new DefensiveStats
            {
                Life = 6500,
                Armor = 15000,
                FireResistance = 75,
                ColdResistance = 75,
                LightningResistance = 75,
                ChaosResistance = 60,
                Spirit = 120
            }
        };

        // Act
        var score = _scorer.ScoreGear(character);

        // Assert
        Assert.InRange(score, 0.8, 1.0);
    }

    [Fact]
    public void ScoreGear_WithPoorGear_ReturnsLowScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "TestChar",
            Stats = new DefensiveStats
            {
                Life = 2000,
                Armor = 500,
                FireResistance = 20,
                ColdResistance = 15,
                LightningResistance = 25,
                ChaosResistance = -30,
                Spirit = 0
            }
        };

        // Act
        var score = _scorer.ScoreGear(character);

        // Assert
        Assert.InRange(score, 0.0, 0.6);
    }

    [Fact]
    public void ScorePassiveTree_WithOptimalAllocation_ReturnsHighScore()
    {
        // Arrange
        var level = 80;
        var expectedPassives = 8 + 80; // BASE_PASSIVES + level
        var passiveCount = expectedPassives; // Perfect allocation

        // Act
        var score = _scorer.ScorePassiveTree(passiveCount, level);

        // Assert
        Assert.Equal(1.0, score, precision: 2);
    }

    [Fact]
    public void ScorePassiveTree_WithUnderAllocation_ReturnsLowerScore()
    {
        // Arrange
        var level = 80;
        var passiveCount = 40; // Significantly under-allocated

        // Act
        var score = _scorer.ScorePassiveTree(passiveCount, level);

        // Assert
        Assert.InRange(score, 0.2, 0.6);
    }

    [Fact]
    public void ScoreSkills_WithOptimalSetup_ReturnsHighScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "TestChar",
            Skills = new List<SkillData>
            {
                new() { Name = "Main Attack", Gems = new() 
                    { 
                        new() { IsSupport = true },
                        new() { IsSupport = true },
                        new() { IsSupport = true }
                    } 
                },
                new() { Name = "Movement", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Aura 1", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Aura 2", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Guard", Gems = new() { new() { IsSupport = true } } }
            }
        };

        // Act
        var score = _scorer.ScoreSkills(character);

        // Assert
        Assert.InRange(score, 0.8, 1.0);
    }

    [Fact]
    public void ScoreSkills_WithNoSupports_ReturnsLowerScore()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "TestChar",
            Skills = new List<SkillData>
            {
                new() { Name = "Main Attack", Gems = new() },
                new() { Name = "Movement", Gems = new() },
                new() { Name = "Aura", Gems = new() }
            }
        };

        // Act
        var score = _scorer.ScoreSkills(character);

        // Assert
        Assert.InRange(score, 0.5, 0.7);
    }

    [Fact]
    public void CalculateDefenseRating_WithStrongDefenses_ReturnsHighRating()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 6500,
            Armor = 15000,
            EnergyShield = 1500,
            BlockChance = 60,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 50
        };

        // Act
        var rating = _scorer.CalculateDefenseRating(stats);

        // Assert
        Assert.InRange(rating, 0.8, 1.0);
    }

    [Fact]
    public void CalculateDefenseRating_WithWeakDefenses_ReturnsLowRating()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2000,
            Armor = 500,
            EnergyShield = 0,
            BlockChance = 0,
            FireResistance = 20,
            ColdResistance = 15,
            LightningResistance = 25,
            ChaosResistance = -30
        };

        // Act
        var rating = _scorer.CalculateDefenseRating(stats);

        // Assert
        Assert.InRange(rating, 0.0, 0.3);
    }

    [Fact]
    public void AnalyzeBuild_IdentifiesTier_BasedOnScore()
    {
        // Test S tier
        var sCharacter = new CharacterData
        {
            Name = "STier",
            Class = "Warrior",
            Level = 90,
            Stats = new DefensiveStats
            {
                Life = 7000,
                Armor = 20000,
                FireResistance = 75,
                ColdResistance = 75,
                LightningResistance = 75,
                ChaosResistance = 75,
                Spirit = 150
            },
            Passives = Enumerable.Range(1, 98).ToList(),
            Skills = Enumerable.Range(1, 5).Select(i => new SkillData 
            { 
                Name = $"Skill{i}", 
                Gems = new() { new() { IsSupport = true } } 
            }).ToList()
        };

        var sResult = _scorer.AnalyzeBuild(sCharacter);
        Assert.True(sResult.Tier is BuildTier.S or BuildTier.A, "Excellent build should be S or A tier");
    }

    [Fact]
    public void AnalyzeBuild_WithBalancedBuild_HasBalancedStrength()
    {
        // Arrange
        var character = new CharacterData
        {
            Name = "Balanced",
            Class = "Ranger",
            Level = 75,
            Stats = new DefensiveStats
            {
                Life = 4800,
                Evasion = 12000,
                FireResistance = 75,
                ColdResistance = 75,
                LightningResistance = 75,
                ChaosResistance = 30,
                Spirit = 80
            },
            Passives = Enumerable.Range(1, 83).ToList(),
            Skills = new List<SkillData>
            {
                new() { Name = "Attack", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Movement", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Aura", Gems = new() { new() { IsSupport = true } } },
                new() { Name = "Guard", Gems = new() { new() { IsSupport = true } } }
            }
        };

        // Act
        var result = _scorer.AnalyzeBuild(character);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.GearScore, 0.6, 0.9);
        Assert.InRange(result.PassiveScore, 0.8, 1.0);
        Assert.InRange(result.SkillScore, 0.7, 1.0);
        Assert.True(result.Ehp > 0, "Should calculate EHP");
    }
}
