using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Interface for damage calculator.
/// </summary>
public interface IDamageCalculator
{
    /// <summary>
    /// Calculate base damage from weapon or spell with added flat damage.
    /// </summary>
    DamageComponents CalculateBaseDamage(
        DamageRange? weaponDamage = null,
        DamageRange? spellBaseDamage = null,
        IEnumerable<(DamageType Type, DamageRange Range)>? addedFlatDamage = null);
    
    /// <summary>
    /// Apply increased/reduced modifiers (additive).
    /// </summary>
    double ApplyIncreasedModifiers(double baseValue, IEnumerable<Modifier> modifiers);
    
    /// <summary>
    /// Apply more/less modifiers (multiplicative).
    /// </summary>
    double ApplyMoreModifiers(double baseValue, IEnumerable<Modifier> modifiers);
    
    /// <summary>
    /// Apply damage conversion between types.
    /// </summary>
    DamageComponents ApplyDamageConversion(
        DamageComponents components,
        Dictionary<DamageType, Dictionary<DamageType, double>> conversions);
    
    /// <summary>
    /// Calculate final damage after applying all modifiers.
    /// </summary>
    DamageRange CalculateFinalDamage(
        DamageRange baseDamage,
        IEnumerable<Modifier>? increasedModifiers = null,
        IEnumerable<Modifier>? moreModifiers = null);
    
    /// <summary>
    /// Calculate damage with critical strike multiplier applied.
    /// </summary>
    DamageRange CalculateCriticalDamage(
        DamageRange baseDamage,
        CriticalStrikeConfig critConfig);
    
    /// <summary>
    /// Calculate attacks per second from base attack time and modifiers.
    /// </summary>
    double CalculateAttackSpeed(
        double baseAttackTime,
        IEnumerable<Modifier> increasedSpeedModifiers);
    
    /// <summary>
    /// Calculate casts per second from base cast time and modifiers.
    /// </summary>
    double CalculateCastSpeed(
        double baseCastTime,
        IEnumerable<Modifier> increasedSpeedModifiers);
    
    /// <summary>
    /// Calculate damage per second (DPS).
    /// </summary>
    double CalculateDps(
        DamageRange damagePerHit,
        double actionsPerSecond,
        CriticalStrikeConfig? critConfig = null);
    
    /// <summary>
    /// Calculate complete DPS with all modifiers for all damage types.
    /// </summary>
    DpsCalculationResult CalculateFullDps(
        DamageComponents baseDamageComponents,
        IEnumerable<Modifier>? increasedDamageModifiers = null,
        IEnumerable<Modifier>? moreDamageModifiers = null,
        double baseActionTime = 1.0,
        IEnumerable<Modifier>? increasedSpeedModifiers = null,
        CriticalStrikeConfig? critConfig = null,
        bool isSpell = false);
}
