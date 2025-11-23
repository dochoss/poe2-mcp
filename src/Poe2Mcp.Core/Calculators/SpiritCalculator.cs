using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Advanced Spirit calculator for Path of Exile 2.
/// Provides comprehensive Spirit management including multiple sources,
/// reservation tracking, support gem optimization, and overflow detection.
/// </summary>
public class SpiritCalculator : ISpiritCalculator
{
    private readonly ILogger<SpiritCalculator> _logger;
    private readonly List<SpiritSource> _sources = new();
    private readonly List<SpiritReservation> _reservations = new();

    public SpiritCalculator(ILogger<SpiritCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SpiritCalculator initialized");
    }

    // Spirit Sources Management
    
    public void AddQuestSpirit(string questName, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questName);
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        var source = new SpiritSource
        {
            Name = questName,
            Amount = amount,
            SourceType = SpiritSourceType.Quest
        };
        _sources.Add(source);
        _logger.LogInformation("Added quest Spirit: {QuestName} (+{Amount})", questName, amount);
    }

    public void AddGearSpirit(string itemName, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        var source = new SpiritSource
        {
            Name = itemName,
            Amount = amount,
            SourceType = SpiritSourceType.Gear
        };
        _sources.Add(source);
        _logger.LogInformation("Added gear Spirit: {ItemName} (+{Amount})", itemName, amount);
    }

    public void AddPassiveSpirit(string nodeName, int amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        var source = new SpiritSource
        {
            Name = nodeName,
            Amount = amount,
            SourceType = SpiritSourceType.PassiveTree
        };
        _sources.Add(source);
        _logger.LogInformation("Added passive Spirit: {NodeName} (+{Amount})", nodeName, amount);
    }

    public void AddSpiritSource(string name, int amount, SpiritSourceType sourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        var source = new SpiritSource
        {
            Name = name,
            Amount = amount,
            SourceType = sourceType
        };
        _sources.Add(source);
        _logger.LogInformation("Added Spirit source: {Name} (+{Amount}) [{SourceType}]", 
            name, amount, sourceType);
    }

    public bool RemoveSpiritSource(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var initialCount = _sources.Count;
        _sources.RemoveAll(s => s.Name == name);
        
        var removed = _sources.Count < initialCount;
        if (removed)
            _logger.LogInformation("Removed Spirit source: {Name}", name);
        else
            _logger.LogWarning("Spirit source not found: {Name}", name);
            
        return removed;
    }

    public bool ToggleSpiritSource(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var source = _sources.FirstOrDefault(s => s.Name == name);
        if (source == null)
        {
            _logger.LogWarning("Spirit source not found: {Name}", name);
            return false;
        }
        
        source.Enabled = !source.Enabled;
        _logger.LogInformation("Toggled Spirit source '{Name}': {State}", 
            name, source.Enabled ? "enabled" : "disabled");
        return source.Enabled;
    }

    public int GetMaximumSpirit()
    {
        return _sources.Where(s => s.Enabled).Sum(s => s.Amount);
    }

    public int GetSpiritBySourceType(SpiritSourceType sourceType)
    {
        return _sources
            .Where(s => s.Enabled && s.SourceType == sourceType)
            .Sum(s => s.Amount);
    }

    // Reservation Management
    
    public SpiritReservation AddReservation(
        string name,
        int baseCost,
        SpiritReservationType reservationType,
        IEnumerable<(string Name, double Multiplier)>? supportGems = null,
        int priority = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (baseCost < 0)
            throw new ArgumentException("Base cost cannot be negative", nameof(baseCost));
        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));
            
        var supportGemsList = supportGems?
            .Select(sg => new SupportGem { Name = sg.Name, Multiplier = sg.Multiplier })
            .ToList() ?? new List<SupportGem>();
            
        var reservation = new SpiritReservation
        {
            Name = name,
            BaseCost = baseCost,
            ReservationType = reservationType,
            SupportGems = supportGemsList,
            Priority = priority
        };
        
        _reservations.Add(reservation);
        
        _logger.LogInformation(
            "Added Spirit reservation: {Name} (base={BaseCost}, final={FinalCost}, priority={Priority})",
            name, baseCost, reservation.CalculateCost(), priority);
            
        return reservation;
    }

    public bool RemoveReservation(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var initialCount = _reservations.Count;
        _reservations.RemoveAll(r => r.Name == name);
        
        var removed = _reservations.Count < initialCount;
        if (removed)
            _logger.LogInformation("Removed Spirit reservation: {Name}", name);
        else
            _logger.LogWarning("Spirit reservation not found: {Name}", name);
            
        return removed;
    }

    public bool ToggleReservation(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var reservation = _reservations.FirstOrDefault(r => r.Name == name);
        if (reservation == null)
        {
            _logger.LogWarning("Spirit reservation not found: {Name}", name);
            return false;
        }
        
        reservation.Enabled = !reservation.Enabled;
        _logger.LogInformation("Toggled Spirit reservation '{Name}': {State}", 
            name, reservation.Enabled ? "enabled" : "disabled");
        return reservation.Enabled;
    }

    public SpiritReservation? GetReservation(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _reservations.FirstOrDefault(r => r.Name == name);
    }

    public int GetSpiritReserved()
    {
        return _reservations.Sum(r => r.CalculateCost());
    }

    public int GetSpiritAvailable()
    {
        return GetMaximumSpirit() - GetSpiritReserved();
    }

    public bool IsOverflowing()
    {
        return GetSpiritAvailable() < 0;
    }

    public int GetOverflowAmount()
    {
        var available = GetSpiritAvailable();
        return available < 0 ? Math.Abs(available) : 0;
    }

    // Analysis and Reporting
    
    public SpiritSummary GetSpiritSummary()
    {
        var maximum = GetMaximumSpirit();
        var reserved = GetSpiritReserved();
        var available = GetSpiritAvailable();
        var overflowing = available < 0;
        var overflowAmount = available < 0 ? Math.Abs(available) : 0;
        
        // Count reservations by type
        var reservationCounts = new Dictionary<string, int>();
        foreach (var resType in Enum.GetValues<SpiritReservationType>())
        {
            var count = _reservations.Count(r => r.Enabled && r.ReservationType == resType);
            reservationCounts[resType.ToString()] = count;
        }
        
        // Count sources by type
        var sourceBreakdown = new Dictionary<string, int>();
        foreach (var srcType in Enum.GetValues<SpiritSourceType>())
        {
            var amount = GetSpiritBySourceType(srcType);
            sourceBreakdown[srcType.ToString()] = amount;
        }
        
        return new SpiritSummary
        {
            MaximumSpirit = maximum,
            ReservedSpirit = reserved,
            AvailableSpirit = available,
            IsOverflowing = overflowing,
            OverflowAmount = overflowAmount,
            UtilizationPercent = maximum > 0 ? (double)reserved / maximum * 100 : 0,
            SourceBreakdown = sourceBreakdown,
            ReservationCounts = reservationCounts,
            ActiveReservations = _reservations.Count(r => r.Enabled),
            TotalReservations = _reservations.Count
        };
    }

    // Optimization
    
    public IReadOnlyList<SpiritOptimization> GetOptimizationSuggestions(int? targetSpiritToFree = null)
    {
        var suggestions = new List<SpiritOptimization>();
        
        // If no target specified, use overflow amount
        var target = targetSpiritToFree ?? GetOverflowAmount();
        
        if (target <= 0)
            return suggestions;
            
        // Option 1: Disable entire reservations (sorted by priority, then cost)
        foreach (var reservation in _reservations
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CalculateCost()))
        {
            suggestions.Add(new SpiritOptimization
            {
                Description = $"Disable '{reservation.Name}' (priority {reservation.Priority})",
                SpiritSaved = reservation.CalculateCost(),
                ActionType = "disable_reservation",
                Target = reservation.Name
            });
        }
        
        // Option 2: Remove support gems from reservations
        foreach (var reservation in _reservations.Where(r => r.Enabled && r.SupportGems.Count > 0))
        {
            foreach (var supportGem in reservation.SupportGems.OrderByDescending(sg => sg.Multiplier))
            {
                var costWith = reservation.CalculateCost();
                
                // Calculate cost without this support
                var tempMultiplier = 1.0;
                foreach (var sg in reservation.SupportGems.Where(sg => sg.Name != supportGem.Name))
                {
                    tempMultiplier *= sg.Multiplier;
                }
                var costWithout = (int)Math.Ceiling(reservation.BaseCost * tempMultiplier);
                
                var savings = costWith - costWithout;
                if (savings > 0)
                {
                    suggestions.Add(new SpiritOptimization
                    {
                        Description = $"Remove '{supportGem.Name}' from '{reservation.Name}'",
                        SpiritSaved = savings,
                        ActionType = "remove_support",
                        Target = $"{reservation.Name}::{supportGem.Name}"
                    });
                }
            }
        }
        
        // Sort by spirit saved (descending)
        return suggestions.OrderByDescending(s => s.SpiritSaved).ToList();
    }

    public IReadOnlyList<string> AutoResolveOverflow()
    {
        var actions = new List<string>();
        var overflow = GetOverflowAmount();
        
        if (overflow <= 0)
        {
            _logger.LogInformation("No overflow to resolve");
            return actions;
        }
        
        _logger.LogInformation("Auto-resolving {Overflow} Spirit overflow", overflow);
        
        // Sort reservations by priority (highest priority value = least important)
        var sortedReservations = _reservations
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CalculateCost())
            .ToList();
            
        var spiritFreed = 0;
        foreach (var reservation in sortedReservations)
        {
            if (spiritFreed >= overflow)
                break;
                
            var cost = reservation.CalculateCost();
            reservation.Enabled = false;
            spiritFreed += cost;
            
            var action = $"Disabled '{reservation.Name}' (priority {reservation.Priority}, freed {cost} Spirit)";
            actions.Add(action);
            _logger.LogInformation("{Action}", action);
        }
        
        var finalOverflow = GetOverflowAmount();
        if (finalOverflow > 0)
            _logger.LogWarning("Could not fully resolve overflow. Remaining: {Overflow} Spirit", finalOverflow);
        else
            _logger.LogInformation("Successfully resolved Spirit overflow");
            
        return actions;
    }

    public OptimalSpiritConfiguration SuggestOptimalConfiguration()
    {
        var maxSpirit = GetMaximumSpirit();
        
        // Sort reservations by priority (lower = more important)
        var sortedReservations = _reservations
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.CalculateCost())
            .ToList();
            
        // Build optimal configuration
        var enabledReservations = new List<string>();
        var disabledReservations = new List<string>();
        var spiritUsed = 0;
        
        foreach (var reservation in sortedReservations)
        {
            // Calculate cost independently of Enabled state
            double cost = reservation.BaseCost;
            foreach (var gem in reservation.SupportGems)
            {
                cost *= gem.Multiplier;
            }
            cost = Math.Round(cost); // If costs are expected to be integer
            if (spiritUsed + cost <= maxSpirit)
            {
                enabledReservations.Add(reservation.Name);
                spiritUsed += (int)cost;
            }
            else
            {
                disabledReservations.Add(reservation.Name);
            }
        }
        
        return new OptimalSpiritConfiguration
        {
            MaximumSpirit = maxSpirit,
            OptimalSpiritUsed = spiritUsed,
            OptimalSpiritRemaining = maxSpirit - spiritUsed,
            EnabledReservations = enabledReservations,
            DisabledReservations = disabledReservations,
            EfficiencyPercent = maxSpirit > 0 ? (double)spiritUsed / maxSpirit * 100 : 0
        };
    }

    public (bool IsValid, IReadOnlyList<string> Issues) ValidateConfiguration()
    {
        var issues = new List<string>();
        
        // Check for overflow
        if (IsOverflowing())
        {
            var overflow = GetOverflowAmount();
            issues.Add($"Spirit overflow: {overflow} Spirit over maximum");
        }
        
        // Check for duplicate names
        var reservationNames = _reservations.Select(r => r.Name).ToList();
        var duplicateReservations = reservationNames
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        if (duplicateReservations.Any())
        {
            issues.Add($"Duplicate reservation names: {string.Join(", ", duplicateReservations)}");
        }
        
        var sourceNames = _sources.Select(s => s.Name).ToList();
        var duplicateSources = sourceNames
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        if (duplicateSources.Any())
        {
            issues.Add($"Duplicate source names: {string.Join(", ", duplicateSources)}");
        }
        
        // Check for zero maximum Spirit
        if (GetMaximumSpirit() == 0)
        {
            issues.Add("No Spirit sources configured (maximum Spirit is 0)");
        }
        
        // Check for reservations with no support gems but high costs
        foreach (var reservation in _reservations)
        {
            if (reservation.BaseCost > 50 && reservation.SupportGems.Count == 0)
            {
                issues.Add(
                    $"'{reservation.Name}' has high base cost ({reservation.BaseCost}) " +
                    "but no support gems - verify this is correct");
            }
        }
        
        var isValid = issues.Count == 0;
        return (isValid, issues);
    }
}
