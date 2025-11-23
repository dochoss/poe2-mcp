using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Analyzers;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Analyzers;

public class ContentReadinessCheckerTests
{
    private readonly IContentReadinessChecker _checker;

    public ContentReadinessCheckerTests()
    {
        var ehpCalculator = new EhpCalculator();
        _checker = new ContentReadinessChecker(
            NullLogger<ContentReadinessChecker>.Instance,
            ehpCalculator);
    }

    [Fact]
    public void CheckReadiness_ForCampaign_WithBasicStats_ReturnsReady()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2000,
            FireResistance = 50,
            ColdResistance = 50,
            LightningResistance = 50,
            ChaosResistance = -30
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.Campaign);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("Campaign", report.ContentName);
        Assert.True(
            report.Readiness is ReadinessLevel.Ready or ReadinessLevel.MostlyReady,
            "Should be ready for campaign");
        Assert.True(report.Confidence >= 60, "Should have reasonable confidence");
    }

    [Fact]
    public void CheckReadiness_ForPinnacleMaps_WithPoorStats_ReturnsNotReady()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 3000,
            Armor = 2000,
            FireResistance = 50,
            ColdResistance = 50,
            LightningResistance = 50,
            ChaosResistance = -20
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.PinnacleMaps);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("Pinnacle Maps (T16+)", report.ContentName);
        Assert.Equal(ReadinessLevel.NotReady, report.Readiness);
        Assert.True(report.Gaps.Count > 0, "Should have identified gaps");
        Assert.True(report.PriorityUpgrades.Count > 0, "Should have priority upgrades");
    }

    [Fact]
    public void CheckReadiness_ForHighMaps_WithGoodStats_ReturnsReady()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5500,
            Armor = 15000,
            EnergyShield = 0,
            FireResistance = 85,
            ColdResistance = 85,
            LightningResistance = 85,
            ChaosResistance = 25
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.HighMaps, dps: 120000);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("High Maps (T11-T15)", report.ContentName);
        Assert.True(
            report.Readiness is ReadinessLevel.Ready or ReadinessLevel.MostlyReady,
            "Should be ready for high maps");
        Assert.True(report.Passes.Count > 0, "Should have passed checks");
        Assert.Equal("pass", report.LifeCheck);
        Assert.Equal("pass", report.ResistanceCheck);
    }

    [Fact]
    public void CheckReadiness_WithUncappedResistances_IdentifiesGaps()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 4000,
            FireResistance = 50,
            ColdResistance = 60,
            LightningResistance = 40,
            ChaosResistance = -30
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.MidMaps);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("fail", report.ResistanceCheck);
        Assert.Contains(report.Gaps, g => g.Contains("Fire Res") || g.Contains("Lightning Res"));
    }

    [Fact]
    public void CheckReadiness_WithLowLife_IdentifiesCriticalGap()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2500,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.MidMaps);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("fail", report.LifeCheck);
        Assert.Contains(report.Gaps, g => g.Contains("Life") && g.Contains("CRITICAL"));
        Assert.Contains(report.PriorityUpgrades, u => u.Contains("life"));
    }

    [Fact]
    public void CheckReadiness_WithWarningLevel_ReturnsRiskyOrMostlyReady()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 3200,
            Armor = 5000,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = -25
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.MidMaps, dps: 30000);

        // Assert
        Assert.NotNull(report);
        Assert.True(
            report.Readiness is ReadinessLevel.MostlyReady or ReadinessLevel.Risky,
            "Should be mostly ready or risky");
        Assert.True(report.Warnings.Count > 0, "Should have warnings");
    }

    [Fact]
    public void CheckReadiness_CalculatesConfidence_BasedOnChecks()
    {
        // Arrange - Perfect stats
        var perfectStats = new DefensiveStats
        {
            Life = 7000,
            Armor = 20000,
            FireResistance = 90,
            ColdResistance = 90,
            LightningResistance = 90,
            ChaosResistance = 50
        };

        // Act
        var perfectReport = _checker.CheckReadiness(perfectStats, ContentDifficulty.HighMaps, dps: 150000);

        // Assert
        Assert.True(perfectReport.Confidence >= 80, "Perfect stats should have high confidence");
    }

    [Fact]
    public void CheckReadiness_ProvidesTips_ForContent()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2500,
            FireResistance = 65,
            ColdResistance = 65,
            LightningResistance = 65,
            ChaosResistance = -40
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.EarlyMaps);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.Recommendations.Count > 0, "Should have recommendations");
        Assert.True(report.Recommendations.Any(r => r.Contains("resistance") || r.Contains("life")),
            "Should include relevant recommendations");
    }

    [Fact]
    public void GetRequirements_ReturnsCorrectRequirements_ForEachDifficulty()
    {
        // Act & Assert for each difficulty
        var campaignReq = _checker.GetRequirements(ContentDifficulty.Campaign);
        Assert.Equal("Campaign", campaignReq.ContentName);
        Assert.True(campaignReq.MinLife > 0);

        var earlyMapsReq = _checker.GetRequirements(ContentDifficulty.EarlyMaps);
        Assert.Equal("Early Maps (T1-T5)", earlyMapsReq.ContentName);
        Assert.True(earlyMapsReq.MinLife > campaignReq.MinLife, "Early maps should require more life");

        var pinnacleReq = _checker.GetRequirements(ContentDifficulty.PinnacleMaps);
        Assert.Equal("Pinnacle Maps (T16+)", pinnacleReq.ContentName);
        Assert.True(pinnacleReq.MinLife > earlyMapsReq.MinLife, "Pinnacle should require most life");
    }

    [Fact]
    public void GetAllRequirements_ReturnsAllContentDifficulties()
    {
        // Act
        var allReqs = _checker.GetAllRequirements();

        // Assert
        Assert.NotNull(allReqs);
        Assert.True(allReqs.Count >= 7, "Should have requirements for all content types");
        Assert.True(allReqs.ContainsKey(ContentDifficulty.Campaign));
        Assert.True(allReqs.ContainsKey(ContentDifficulty.EarlyMaps));
        Assert.True(allReqs.ContainsKey(ContentDifficulty.PinnacleMaps));
        Assert.True(allReqs.ContainsKey(ContentDifficulty.BossPinnacle));
    }

    [Fact]
    public void CheckReadiness_ForBosses_RequiresHigherDefenses()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            Armor = 10000,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 15
        };

        // Act - Normal boss
        var normalBossReport = _checker.CheckReadiness(stats, ContentDifficulty.BossNormal, dps: 50000);

        // Act - Pinnacle boss
        var pinnacleBossReport = _checker.CheckReadiness(stats, ContentDifficulty.BossPinnacle, dps: 50000);

        // Assert
        Assert.True(
            normalBossReport.Readiness is ReadinessLevel.Ready or ReadinessLevel.MostlyReady,
            "Should be ready for normal bosses");
        Assert.Equal(ReadinessLevel.NotReady, pinnacleBossReport.Readiness);
        Assert.True(pinnacleBossReport.Gaps.Count > normalBossReport.Gaps.Count,
            "Pinnacle bosses should have more gaps");
    }

    [Fact]
    public void CheckReadiness_WithNoDps_StillCalculatesReadiness()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 4500,
            Armor = 12000,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };

        // Act - No DPS provided
        var report = _checker.CheckReadiness(stats, ContentDifficulty.MidMaps);

        // Assert
        Assert.NotNull(report);
        Assert.Equal("warning", report.DamageCheck); // Should warn when DPS not calculated
        Assert.Contains(report.Warnings, w => w.Contains("DPS"));
    }

    [Fact]
    public void CheckReadiness_GeneratesPriorityUpgrades_BasedOnGaps()
    {
        // Arrange - Multiple gaps
        var stats = new DefensiveStats
        {
            Life = 2000,
            Armor = 500,
            FireResistance = 40,
            ColdResistance = 45,
            LightningResistance = 50,
            ChaosResistance = -60
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.MidMaps);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.PriorityUpgrades.Count > 0, "Should have priority upgrades");
        Assert.Contains(report.PriorityUpgrades, u => u.Contains("PRIORITY"));
    }

    [Fact]
    public void CheckReadiness_IdentifiesStrengths_WhenMeetingRequirements()
    {
        // Arrange - Excellent stats
        var stats = new DefensiveStats
        {
            Life = 5000,
            Armor = 15000,
            EnergyShield = 1500,
            BlockChance = 70,
            FireResistance = 85,
            ColdResistance = 85,
            LightningResistance = 85,
            ChaosResistance = 30
        };

        // Act
        var report = _checker.CheckReadiness(stats, ContentDifficulty.HighMaps, dps: 120000);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.Passes.Count > 0, "Should identify strengths");
        Assert.Contains(report.Passes, p => p.Contains("Life") || p.Contains("resistance"));
    }
}
