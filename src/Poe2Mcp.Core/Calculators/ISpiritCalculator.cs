using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Calculator for Path of Exile 2 Spirit resource management.
/// Spirit limits permanent minions, auras, and meta-gems in PoE2.
/// </summary>
public interface ISpiritCalculator
{
    /// <summary>
    /// Add Spirit from a quest reward.
    /// </summary>
    /// <param name="questName">Name of the quest.</param>
    /// <param name="amount">Spirit amount (typically 30, 30, or 40).</param>
    void AddQuestSpirit(string questName, int amount);
    
    /// <summary>
    /// Add Spirit from gear.
    /// </summary>
    /// <param name="itemName">Name of the item.</param>
    /// <param name="amount">Spirit bonus from item.</param>
    void AddGearSpirit(string itemName, int amount);
    
    /// <summary>
    /// Add Spirit from passive tree.
    /// </summary>
    /// <param name="nodeName">Name of the passive node.</param>
    /// <param name="amount">Spirit bonus from node.</param>
    void AddPassiveSpirit(string nodeName, int amount);
    
    /// <summary>
    /// Add a generic Spirit source.
    /// </summary>
    /// <param name="name">Source name.</param>
    /// <param name="amount">Spirit amount.</param>
    /// <param name="sourceType">Type of source.</param>
    void AddSpiritSource(string name, int amount, SpiritSourceType sourceType);
    
    /// <summary>
    /// Remove a Spirit source by name.
    /// </summary>
    /// <param name="name">Source name to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveSpiritSource(string name);
    
    /// <summary>
    /// Toggle a Spirit source on/off.
    /// </summary>
    /// <param name="name">Source name to toggle.</param>
    /// <returns>New enabled state, or false if not found.</returns>
    bool ToggleSpiritSource(string name);
    
    /// <summary>
    /// Calculate maximum Spirit from all active sources.
    /// </summary>
    /// <returns>Total maximum Spirit.</returns>
    int GetMaximumSpirit();
    
    /// <summary>
    /// Get Spirit from a specific source type.
    /// </summary>
    /// <param name="sourceType">Type of source to sum.</param>
    /// <returns>Total Spirit from that source type.</returns>
    int GetSpiritBySourceType(SpiritSourceType sourceType);
    
    /// <summary>
    /// Add a Spirit reservation.
    /// </summary>
    /// <param name="name">Reservation name.</param>
    /// <param name="baseCost">Base Spirit cost.</param>
    /// <param name="reservationType">Type of reservation.</param>
    /// <param name="supportGems">Optional list of (name, multiplier) tuples.</param>
    /// <param name="priority">Priority (1-10, lower = more important).</param>
    /// <returns>The created SpiritReservation.</returns>
    SpiritReservation AddReservation(
        string name,
        int baseCost,
        SpiritReservationType reservationType,
        IEnumerable<(string Name, double Multiplier)>? supportGems = null,
        int priority = 5);
    
    /// <summary>
    /// Remove a Spirit reservation by name.
    /// </summary>
    /// <param name="name">Reservation name to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveReservation(string name);
    
    /// <summary>
    /// Toggle a Spirit reservation on/off.
    /// </summary>
    /// <param name="name">Reservation name to toggle.</param>
    /// <returns>New enabled state, or false if not found.</returns>
    bool ToggleReservation(string name);
    
    /// <summary>
    /// Get a reservation by name.
    /// </summary>
    /// <param name="name">Reservation name.</param>
    /// <returns>SpiritReservation if found, null otherwise.</returns>
    SpiritReservation? GetReservation(string name);
    
    /// <summary>
    /// Calculate total Spirit reserved by all active reservations.
    /// </summary>
    /// <returns>Total Spirit reserved.</returns>
    int GetSpiritReserved();
    
    /// <summary>
    /// Calculate available Spirit after reservations.
    /// </summary>
    /// <returns>Available Spirit (can be negative if over-reserved).</returns>
    int GetSpiritAvailable();
    
    /// <summary>
    /// Check if Spirit reservations exceed maximum.
    /// </summary>
    /// <returns>True if over-reserved, false otherwise.</returns>
    bool IsOverflowing();
    
    /// <summary>
    /// Get amount of Spirit overflow (if any).
    /// </summary>
    /// <returns>Overflow amount (0 if not overflowing).</returns>
    int GetOverflowAmount();
    
    /// <summary>
    /// Get comprehensive Spirit summary.
    /// </summary>
    /// <returns>Complete Spirit status.</returns>
    SpiritSummary GetSpiritSummary();
    
    /// <summary>
    /// Get suggestions for optimizing Spirit usage.
    /// </summary>
    /// <param name="targetSpiritToFree">How much Spirit needs to be freed (null = all overflow).</param>
    /// <returns>List of optimization suggestions sorted by effectiveness.</returns>
    IReadOnlyList<SpiritOptimization> GetOptimizationSuggestions(int? targetSpiritToFree = null);
    
    /// <summary>
    /// Automatically resolve Spirit overflow by disabling lowest priority reservations.
    /// </summary>
    /// <returns>List of actions taken.</returns>
    IReadOnlyList<string> AutoResolveOverflow();
    
    /// <summary>
    /// Suggest optimal Spirit configuration to maximize usage without overflow.
    /// </summary>
    /// <returns>Optimal configuration suggestion.</returns>
    OptimalSpiritConfiguration SuggestOptimalConfiguration();
    
    /// <summary>
    /// Validate current Spirit configuration.
    /// </summary>
    /// <returns>Tuple of (is_valid, list_of_issues).</returns>
    (bool IsValid, IReadOnlyList<string> Issues) ValidateConfiguration();
}
