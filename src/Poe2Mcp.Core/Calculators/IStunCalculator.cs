using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Interface for stun calculator.
/// </summary>
public interface IStunCalculator
{
    /// <summary>
    /// Calculate Light Stun chance for a hit.
    /// </summary>
    LightStunResult CalculateLightStunChance(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        StunModifiers? modifiers = null);
    
    /// <summary>
    /// Calculate Heavy Stun buildup for a hit and update the entity's meter.
    /// </summary>
    HeavyStunResult CalculateHeavyStunBuildup(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        string entityId = "default",
        StunModifiers? modifiers = null,
        bool lightStunWouldOccur = false);
    
    /// <summary>
    /// Calculate both Light Stun and Heavy Stun for a single hit.
    /// </summary>
    CompleteStunResult CalculateCompleteStun(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        string entityId = "default",
        StunModifiers? modifiers = null);
    
    /// <summary>
    /// Get the Heavy Stun meter for an entity.
    /// </summary>
    HeavyStunMeter? GetHeavyStunMeter(string entityId = "default");
    
    /// <summary>
    /// Reset the Heavy Stun meter for an entity.
    /// </summary>
    void ResetHeavyStunMeter(string entityId = "default");
    
    /// <summary>
    /// Remove an entity's Heavy Stun meter from tracking.
    /// </summary>
    void RemoveEntity(string entityId);
    
    /// <summary>
    /// Get a list of all entities currently being tracked.
    /// </summary>
    IReadOnlyList<string> GetAllTrackedEntities();
    
    /// <summary>
    /// Calculate theoretical hits needed for both Light and Heavy stun.
    /// </summary>
    (double HitsForLightStun, double HitsForHeavyStun) CalculateHitsToStun(
        double damagePerHit,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        StunModifiers? modifiers = null);
}
