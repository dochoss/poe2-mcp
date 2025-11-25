# API Documentation

This document describes the MCP tools exposed by the PoE2 Build Optimizer server.

## Overview

The server implements the Model Context Protocol (MCP) and exposes 27 tools for Path of Exile 2 character analysis and optimization.

## Tool Categories

### Character Tools

#### `analyze_character`

Analyze a Path of Exile 2 character's build, defenses, and optimization opportunities.

**Parameters:**
- `account` (string, required): Account name from pathofexile.com profile
- `character` (string, required): Character name
- `league` (string, optional): League name (default: "Standard")

**Returns:**
- Character summary
- Defensive stats analysis
- Offensive capabilities
- Improvement suggestions

**Example:**
```
Analyze the character "MyWarrior" on account "ExamplePlayer"
```

#### `compare_to_top_players`

Compare a character to top ladder players of the same build/class.

**Parameters:**
- `account` (string, required): Account name
- `character` (string, required): Character name
- `league` (string, optional): League name
- `comparison_count` (number, optional): Number of players to compare (default: 10)

**Returns:**
- Percentile rankings
- Stat comparisons
- Gear differences
- Improvement recommendations

---

### Calculator Tools

#### `calculate_character_ehp`

Calculate Effective Health Pool against all damage types.

**Parameters:**
- `life` (number, required): Maximum life
- `energy_shield` (number, optional): Maximum energy shield
- `armor` (number, optional): Armor value
- `evasion` (number, optional): Evasion rating
- `block_chance` (number, optional): Block chance (0-50)
- `fire_resistance` (number, optional): Fire resistance
- `cold_resistance` (number, optional): Cold resistance
- `lightning_resistance` (number, optional): Lightning resistance
- `chaos_resistance` (number, optional): Chaos resistance
- `expected_hit_size` (number, optional): Expected hit size for armor calculations (default: 1000)

**Returns:**
- EHP against each damage type
- Mitigation breakdown
- Defense multipliers

#### `analyze_spirit_usage`

Analyze Spirit allocation and reservations.

**Parameters:**
- `maximum_spirit` (number, required): Total available Spirit
- `reservations` (array, required): List of Spirit reservations
  - `name` (string): Skill/aura name
  - `base_cost` (number): Base Spirit cost
  - `support_multipliers` (array, optional): Support gem multipliers

**Returns:**
- Current Spirit usage
- Available Spirit
- Overflow detection
- Optimization suggestions

#### `analyze_stun_vulnerability`

Analyze stun mechanics for attacks against targets.

**Parameters:**
- `damage` (number, required): Damage dealt
- `target_max_life` (number, required): Target's maximum life
- `damage_type` (string, required): "physical", "fire", "cold", "lightning", "chaos"
- `attack_type` (string, required): "melee", "ranged", "spell"

**Returns:**
- Light stun chance
- Heavy stun buildup
- Hits to stun calculation
- Crushing blow potential

#### `calculate_dps`

Calculate comprehensive DPS breakdown.

**Parameters:**
- `base_damage_min` (number, required): Minimum base damage
- `base_damage_max` (number, required): Maximum base damage
- `attack_speed` (number, optional): Attacks per second (default: 1.0)
- `crit_chance` (number, optional): Critical strike chance
- `crit_multiplier` (number, optional): Critical strike multiplier
- `increased_damage` (array, optional): List of increased damage modifiers
- `more_damage` (array, optional): List of more damage multipliers

**Returns:**
- Total DPS
- Average hit
- DPS by damage type
- Modifier breakdown

---

### Analyzer Tools

#### `detect_character_weaknesses`

Find vulnerabilities in a character build.

**Parameters:**
- `defensive_stats` (object, required): Character's defensive statistics
- `include_suggestions` (boolean, optional): Include improvement suggestions (default: true)

**Returns:**
- List of weaknesses by severity
- Suggested fixes
- Priority order

#### `evaluate_gear_upgrade`

Compare two items to determine upgrade value.

**Parameters:**
- `current_item` (object, required): Currently equipped item
- `new_item` (object, required): Potential replacement item
- `character_context` (object, optional): Character stats for context

**Returns:**
- Overall upgrade score
- Stat-by-stat comparison
- EHP impact
- DPS impact (if applicable)

#### `optimize_build_metrics`

Get comprehensive build optimization recommendations.

**Parameters:**
- `character` (object, required): Character data
- `optimization_goal` (string, optional): "balanced", "defense", "offense", "boss", "mapping"

**Returns:**
- Current build score
- Tier rating (S through F)
- Recommended upgrades
- Priority list

#### `check_content_readiness`

Check if a build is ready for specific content.

**Parameters:**
- `defensive_stats` (object, required): Character defenses
- `dps` (number, optional): Character DPS
- `content_type` (string, optional): "campaign", "early_maps", "mid_maps", "red_maps", "pinnacle_bosses", "uber_bosses"

**Returns:**
- Readiness status
- Gap analysis
- Minimum requirements
- Recommended improvements

---

### Optimizer Tools

#### `optimize_gear`

Get gear upgrade recommendations within budget.

**Parameters:**
- `character` (object, required): Character data
- `budget_tier` (string, optional): "low", "medium", "high", "unlimited"
- `slots_to_upgrade` (array, optional): Specific slots to focus on

**Returns:**
- Prioritized upgrade list
- Budget allocations
- Expected improvements
- Trade search suggestions

#### `optimize_passive_tree`

Suggest passive tree optimizations.

**Parameters:**
- `current_passives` (array, required): Currently allocated passives
- `character_class` (string, required): Character class
- `optimization_goal` (string, optional): What to optimize for

**Returns:**
- Suggested allocations
- Potential respec targets
- Efficiency analysis
- Point redistribution plan

#### `optimize_skills`

Optimize skill gem setup.

**Parameters:**
- `current_skills` (array, required): Currently equipped skills
- `available_spirit` (number, required): Available Spirit
- `build_type` (string, optional): Build archetype

**Returns:**
- Recommended skill setup
- Support gem combinations
- Link priorities
- Spirit allocation

#### `find_best_supports`

Find optimal support gem combinations.

**Parameters:**
- `skill_name` (string, required): Main skill to support
- `max_supports` (number, optional): Maximum number of supports (default: 5)
- `available_spirit` (number, optional): Spirit budget

**Returns:**
- Ranked support combinations
- DPS calculations
- Spirit costs
- Synergy analysis

---

### AI Tools

#### `natural_language_query`

Answer questions about PoE2 using AI.

**Parameters:**
- `query` (string, required): Natural language question
- `character_context` (object, optional): Character data for context

**Returns:**
- AI-generated answer
- Source citations
- Related mechanics

**Example Questions:**
- "How does armor work in PoE2?"
- "What's the best support for Earthquake?"
- "Should I use ES or Life for my build?"

#### `explain_mechanic`

Get detailed explanation of a game mechanic.

**Parameters:**
- `mechanic` (string, required): Mechanic name (e.g., "freeze", "crit", "spirit")
- `include_examples` (boolean, optional): Include practical examples

**Returns:**
- Mechanic explanation
- Formula (if applicable)
- Tips and tricks
- Common questions

**Available Mechanics:**
- `freeze` - Freeze ailment
- `shock` - Shock ailment
- `stun` - Stun mechanics
- `crit` - Critical strikes
- `spirit` - Spirit system
- `increased_vs_more` - Modifier types

#### `compare_items`

AI-powered item comparison.

**Parameters:**
- `item1` (object, required): First item
- `item2` (object, required): Second item
- `build_context` (string, optional): Build type for context

**Returns:**
- Comparison summary
- Winner recommendation
- Situational analysis

---

### Utility Tools

#### `health_check`

Check server health and component status.

**Parameters:** None

**Returns:**
- Server status
- Component health
- Version info

#### `clear_cache`

Clear cached data.

**Parameters:**
- `cache_type` (string, optional): "all", "memory", "database"

**Returns:**
- Cleared entries count
- Cache statistics

#### `search_trade_items`

Search trade site for items.

**Parameters:**
- `item_type` (string, required): Item base type
- `modifiers` (array, optional): Required modifiers
- `min_price` (number, optional): Minimum price
- `max_price` (number, optional): Maximum price

**Returns:**
- Item listings
- Price statistics
- Sort by value

#### `import_pob`

Import Path of Building code.

**Parameters:**
- `pob_code` (string, required): Path of Building export code

**Returns:**
- Parsed character data
- Item details
- Passive tree

#### `export_pob`

Export character to Path of Building format.

**Parameters:**
- `character` (object, required): Character data

**Returns:**
- PoB export code
- XML data

#### `get_pob_code`

Get PoB code for a character.

**Parameters:**
- `account` (string, required): Account name
- `character` (string, required): Character name

**Returns:**
- PoB code string

#### `setup_trade_auth`

Configure trade site authentication.

**Parameters:**
- `session_id` (string, required): POESESSID cookie

**Returns:**
- Authentication status

#### `compare_builds`

Compare two builds.

**Parameters:**
- `build1` (object, required): First build
- `build2` (object, required): Second build

**Returns:**
- Side-by-side comparison
- Strengths/weaknesses
- Recommendations

#### `search_items`

Search item database.

**Parameters:**
- `query` (string, required): Search query
- `item_class` (string, optional): Filter by class
- `max_results` (number, optional): Maximum results

**Returns:**
- Matching items
- Item details

#### `analyze_damage_scaling`

Analyze damage scaling factors.

**Parameters:**
- `base_damage` (number, required): Base damage
- `modifiers` (array, required): All damage modifiers

**Returns:**
- Scaling breakdown
- Bottleneck analysis
- Optimization suggestions

---

## Error Handling

All tools return errors in a consistent format:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_PARAMETER",
    "message": "Account name is required"
  }
}
```

Common error codes:
- `INVALID_PARAMETER` - Missing or invalid parameter
- `NOT_FOUND` - Resource not found
- `RATE_LIMITED` - API rate limit exceeded
- `INTERNAL_ERROR` - Server error

## Rate Limits

The server implements rate limiting for external APIs:

- PoE API: 10 requests/second
- poe.ninja: 5 requests/second
- Trade API: 5 requests/second

Cached data is used when available to minimize API calls.

---

For deployment instructions, see [DEPLOYMENT.md](./DEPLOYMENT.md).
