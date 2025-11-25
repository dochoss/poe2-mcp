using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Benchmarks.Calculators;

/// <summary>
/// Performance benchmarks for the EHP calculator
/// </summary>
[MemoryDiagnoser]
public class EhpCalculatorBenchmarks
{
    private EhpCalculator _calculator = null!;
    private DefensiveStats _basicStats = null!;
    private DefensiveStats _complexStats = null!;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new EhpCalculator();

        _basicStats = new DefensiveStats
        {
            Life = 5000,
            EnergyShield = 0,
            Armor = 0,
            Evasion = 0,
            BlockChance = 0,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };

        _complexStats = new DefensiveStats
        {
            Life = 5500,
            EnergyShield = 2000,
            Armor = 25000,
            Evasion = 15000,
            BlockChance = 45,
            FireResistance = 78,
            ColdResistance = 76,
            LightningResistance = 80,
            ChaosResistance = 35
        };
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<EhpResult> CalculateEhp_BasicStats()
    {
        return _calculator.CalculateEhpDetailed(_basicStats);
    }

    [Benchmark]
    public IReadOnlyList<EhpResult> CalculateEhp_ComplexStats()
    {
        return _calculator.CalculateEhpDetailed(_complexStats);
    }

    [Benchmark]
    public IReadOnlyList<EhpResult> CalculateEhp_WithCustomHitSize()
    {
        return _calculator.CalculateEhpDetailed(_complexStats, expectedHitSize: 5000);
    }

    [Benchmark]
    public double CalculateResistanceMultiplier()
    {
        return EhpCalculator.CalculateResistanceMultiplier(75);
    }

    [Benchmark]
    public double CalculateArmorDamageReduction()
    {
        return EhpCalculator.CalculateArmorDamageReduction(25000, 1000);
    }
}

/// <summary>
/// Performance benchmarks for the Damage calculator
/// </summary>
[MemoryDiagnoser]
public class DamageCalculatorBenchmarks
{
    private DamageCalculator _calculator = null!;
    private DamageRange _weaponDamage = null!;
    private DamageComponents _components = null!;
    private Modifier[] _increasedModifiers = null!;
    private Modifier[] _moreModifiers = null!;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new DamageCalculator(NullLogger<DamageCalculator>.Instance);

        _weaponDamage = new DamageRange { MinDamage = 100, MaxDamage = 250 };

        _components = new DamageComponents();
        _components.AddDamage(DamageType.Physical, new DamageRange { MinDamage = 100, MaxDamage = 200 });
        _components.AddDamage(DamageType.Fire, new DamageRange { MinDamage = 50, MaxDamage = 100 });
        _components.AddDamage(DamageType.Cold, new DamageRange { MinDamage = 30, MaxDamage = 60 });

        _increasedModifiers = new[]
        {
            new Modifier { Value = 50, ModifierType = ModifierType.Increased },
            new Modifier { Value = 30, ModifierType = ModifierType.Increased },
            new Modifier { Value = 25, ModifierType = ModifierType.Increased },
            new Modifier { Value = 15, ModifierType = ModifierType.Increased }
        };

        _moreModifiers = new[]
        {
            new Modifier { Value = 30, ModifierType = ModifierType.More },
            new Modifier { Value = 20, ModifierType = ModifierType.More },
            new Modifier { Value = 15, ModifierType = ModifierType.More }
        };
    }

    [Benchmark]
    public DamageComponents CalculateBaseDamage()
    {
        return _calculator.CalculateBaseDamage(weaponDamage: _weaponDamage);
    }

    [Benchmark]
    public double ApplyIncreasedModifiers()
    {
        return _calculator.ApplyIncreasedModifiers(100, _increasedModifiers);
    }

    [Benchmark]
    public double ApplyMoreModifiers()
    {
        return _calculator.ApplyMoreModifiers(100, _moreModifiers);
    }

    [Benchmark]
    public DamageRange CalculateFinalDamage()
    {
        return _calculator.CalculateFinalDamage(_weaponDamage, _increasedModifiers, _moreModifiers);
    }

    [Benchmark]
    public DpsCalculationResult CalculateFullDps()
    {
        return _calculator.CalculateFullDps(
            _components,
            _increasedModifiers,
            _moreModifiers,
            baseActionTime: 1.0);
    }
}

/// <summary>
/// Performance benchmarks for the Spirit calculator
/// </summary>
[MemoryDiagnoser]
public class SpiritCalculatorBenchmarks
{
    private SpiritCalculator _calculator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new SpiritCalculator(NullLogger<SpiritCalculator>.Instance);
        
        // Setup typical spirit configuration
        _calculator.AddQuestSpirit("Act 1 Quest", 30);
        _calculator.AddQuestSpirit("Act 2 Quest", 30);
        _calculator.AddQuestSpirit("Act 3 Quest", 40);
        _calculator.AddGearSpirit("Helmet", 35);
        _calculator.AddGearSpirit("Body Armour", 25);
        _calculator.AddPassiveSpirit("Spirit Node 1", 15);
    }

    [Benchmark]
    public int GetMaximumSpirit()
    {
        return _calculator.GetMaximumSpirit();
    }

    [Benchmark]
    public SpiritSummary GetSpiritSummary()
    {
        return _calculator.GetSpiritSummary();
    }

    [Benchmark]
    public IReadOnlyList<SpiritOptimization> GetOptimizationSuggestions()
    {
        // Add some reservations first
        _calculator.AddReservation("Aura 1", 40, SpiritReservationType.Aura, priority: 3);
        _calculator.AddReservation("Aura 2", 35, SpiritReservationType.Aura, priority: 5);
        _calculator.AddReservation("Minion", 30, SpiritReservationType.PermanentMinion, priority: 7);
        
        var suggestions = _calculator.GetOptimizationSuggestions();
        
        // Cleanup
        _calculator.RemoveReservation("Aura 1");
        _calculator.RemoveReservation("Aura 2");
        _calculator.RemoveReservation("Minion");
        
        return suggestions;
    }
}

/// <summary>
/// Performance benchmarks for the Stun calculator
/// </summary>
[MemoryDiagnoser]
public class StunCalculatorBenchmarks
{
    private StunCalculator _calculator = null!;
    private string _entityId = "benchmark_entity";
    private int _counter = 0;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new StunCalculator(NullLogger<StunCalculator>.Instance);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _calculator.RemoveEntity(_entityId);
        _counter = 0;
    }

    [Benchmark]
    public LightStunResult CalculateLightStunChance()
    {
        return _calculator.CalculateLightStunChance(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee);
    }

    [Benchmark]
    public HeavyStunResult CalculateHeavyStunBuildup()
    {
        // Use incrementing counter instead of Guid for consistent benchmarks
        var entityId = $"{_entityId}_{_counter++}";
        return _calculator.CalculateHeavyStunBuildup(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
    }

    [Benchmark]
    public CompleteStunResult CalculateCompleteStun()
    {
        // Use incrementing counter instead of Guid for consistent benchmarks
        var entityId = $"{_entityId}_complete_{_counter++}";
        return _calculator.CalculateCompleteStun(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
    }

    [Benchmark]
    public (double, double) CalculateHitsToStun()
    {
        return _calculator.CalculateHitsToStun(
            500, 5000,
            DamageType.Physical,
            AttackType.Melee);
    }
}
