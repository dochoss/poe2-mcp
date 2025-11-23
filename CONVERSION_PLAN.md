# Python to C# Conversion Plan

## Overview

This document outlines the comprehensive plan for converting the Path of Exile 2 Build Optimizer MCP server from Python to C# .NET 10.

## Project Goals

1. **Preserve Functionality**: All features from the Python version must be available in C#
2. **Modern Best Practices**: Use .NET 10, C# 14, and current MCP SDK patterns
3. **Maintain Python Version**: Keep original Python implementation in `poe2-mcp-python` folder
4. **Type Safety**: Leverage C#'s strong typing for better reliability
5. **Performance**: Take advantage of compiled .NET performance
6. **Enterprise Ready**: Production-grade DI, logging, configuration

## Technology Stack

### Core Technologies
- ✅ **.NET 10** - Long Term Support (LTS) release
- ✅ **C# 14** - Latest language features
- ⏳ **ModelContextProtocol** - Official C# SDK (NuGet package)
- ⏳ **Entity Framework Core 10** - Database ORM
- ✅ **Microsoft.Extensions.Hosting** - Dependency injection
- ⏳ **Microsoft.Extensions.AI** - AI service integration
- ✅ **xUnit** - Testing framework

### Supporting Libraries
- **Microsoft.Extensions.Configuration** - Configuration management
- **Microsoft.Extensions.Logging** - Structured logging
- **Microsoft.Extensions.Caching** - Multi-tier caching
- **System.Text.Json** - JSON serialization
- **SQLite** - Database storage

## Project Structure

```
poe2-mcp/
├── src/
│   ├── Poe2Mcp.Server/           # MCP server application
│   │   ├── Program.cs             # Entry point
│   │   ├── McpServer.cs           # MCP server implementation
│   │   ├── Configuration.cs       # Configuration classes
│   │   ├── appsettings.json       # Configuration
│   │   └── Tools/                 # MCP tool implementations
│   │       ├── CharacterTools.cs
│   │       ├── CalculatorTools.cs
│   │       ├── OptimizerTools.cs
│   │       └── AITools.cs
│   └── Poe2Mcp.Core/             # Core business logic
│       ├── Calculators/           # Game calculators
│       │   ├── EhpCalculator.cs   ✅
│       │   ├── SpiritCalculator.cs
│       │   ├── DamageCalculator.cs
│       │   └── StunCalculator.cs
│       ├── Analyzers/             # Build analyzers
│       │   ├── WeaknessDetector.cs
│       │   ├── GearEvaluator.cs
│       │   ├── BuildScorer.cs
│       │   └── TopPlayerFetcher.cs
│       ├── Optimizers/            # Build optimizers
│       │   ├── GearOptimizer.cs
│       │   ├── PassiveOptimizer.cs
│       │   ├── SkillOptimizer.cs
│       │   └── GemSynergyCalculator.cs
│       ├── Data/                  # Database layer
│       │   ├── Poe2DbContext.cs
│       │   ├── Models/            # EF Core models
│       │   └── Migrations/
│       ├── Services/              # External services
│       │   ├── PoeApiClient.cs
│       │   ├── PoeNinjaApi.cs
│       │   ├── TradeApiClient.cs
│       │   ├── CacheService.cs
│       │   └── RateLimiter.cs
│       ├── AI/                    # AI integration
│       │   ├── QueryHandler.cs
│       │   ├── RecommendationEngine.cs
│       │   └── MechanicsKnowledgeBase.cs
│       └── Models/                # Domain models
│           ├── DefensiveModels.cs ✅
│           ├── CharacterModels.cs
│           ├── ItemModels.cs
│           └── SkillModels.cs
├── tests/
│   └── Poe2Mcp.Tests/
│       ├── Calculators/
│       │   └── EhpCalculatorTests.cs ✅
│       ├── Analyzers/
│       ├── Services/
│       └── Integration/
└── poe2-mcp-python/              # Original Python implementation
    └── (preserved as-is)
```

## Conversion Phases

### Phase 1: Organization & Setup ✅ COMPLETE

**Status**: ✅ All tasks completed

Tasks:
- [x] Move Python files to `poe2-mcp-python` folder
- [x] Create .NET 10 solution with projects
- [x] Set up .gitignore for C# projects
- [x] Create README.md for C# version
- [x] Add NuGet packages to projects
- [x] Verify solution builds
- [x] Create sample calculator with tests

**Deliverables**:
- ✅ Solution builds successfully
- ✅ 3/3 tests passing
- ✅ EHP calculator fully implemented
- ✅ Python code preserved

### Phase 2: Core Infrastructure (IN PROGRESS)

**Status**: ⏳ 20% complete

Tasks:
- [ ] Research ModelContextProtocol C# SDK API
- [ ] Implement MCP server with stdio transport
- [ ] Register tools, resources, and prompts
- [ ] Create Entity Framework Core DbContext
- [ ] Define database models
- [ ] Implement migrations
- [ ] Set up multi-tier caching
- [ ] Configure logging
- [ ] Create appsettings structure

**Key Files to Create**:
```csharp
// Data/Poe2DbContext.cs
public class Poe2DbContext : DbContext
{
    public DbSet<Item> Items { get; set; }
    public DbSet<PassiveNode> PassiveNodes { get; set; }
    public DbSet<SkillGem> SkillGems { get; set; }
    // ... more entities
}

// Services/CacheService.cs
public class CacheService : ICacheService
{
    // L1: Memory cache
    // L2: Redis (optional)
    // L3: SQLite persistent cache
}

// Services/RateLimiter.cs
public class RateLimiter : IRateLimiter
{
    // Token bucket algorithm
    // Per-endpoint configuration
}
```

### Phase 3: API Layer

**Status**: ⏳ 0% complete

Tasks:
- [ ] Port PoE API client (HTTP calls)
- [ ] Port poe.ninja API client
- [ ] Implement character fetcher
- [ ] Port trade API client
- [ ] Create rate limiting middleware
- [ ] Implement caching decorators
- [ ] Add API response models
- [ ] Write integration tests

**Python → C# Mapping**:
```python
# Python
async def get_character(account_name, character_name):
    async with httpx.AsyncClient() as client:
        response = await client.get(url)
        return response.json()
```

```csharp
// C#
public async Task<CharacterData> GetCharacterAsync(
    string accountName, 
    string characterName,
    CancellationToken cancellationToken = default)
{
    using var response = await _httpClient.GetAsync(url, cancellationToken);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<CharacterData>();
}
```

### Phase 4: Calculators & Analyzers

**Status**: ⏳ 12.5% complete (1/8 calculators done)

Tasks:
- [x] EHP Calculator ✅
- [ ] Spirit Calculator
- [ ] Damage Calculator
- [ ] Stun Calculator
- [ ] Weakness Detector
- [ ] Gear Evaluator
- [ ] Build Scorer
- [ ] Content Readiness Checker

**Pattern for Calculator Classes**:
```csharp
public interface IEhpCalculator
{
    IReadOnlyList<EhpResult> CalculateEhp(
        DefensiveStats stats, 
        int expectedHitSize = 1000);
}

public class EhpCalculator : IEhpCalculator
{
    // Implementation with PoE2 formulas
    // Unit tested
    // Well documented
}
```

### Phase 5: Optimizers

**Status**: ⏳ 0% complete

Tasks:
- [ ] Gear Optimizer
- [ ] Passive Tree Optimizer
- [ ] Skill Setup Optimizer
- [ ] Gem Synergy Calculator
- [ ] Gear Comparator
- [ ] Damage Scaling Analyzer

**Key Algorithms**:
- Combinatorial optimization for support gems
- Pathfinding for passive tree
- Weighted scoring for gear recommendations

### Phase 6: AI Integration

**Status**: ⏳ 0% complete

Tasks:
- [ ] Add Microsoft.Extensions.AI package
- [ ] Implement IChatClient wrapper
- [ ] Port query handler
- [ ] Port recommendation engine
- [ ] Port mechanics knowledge base
- [ ] Create prompt templates
- [ ] Add conversation context management

**Example Integration**:
```csharp
public class QueryHandler : IQueryHandler
{
    private readonly IChatClient _chatClient;
    
    public async Task<string> HandleQueryAsync(
        string query,
        CharacterData? context = null)
    {
        var messages = BuildPrompt(query, context);
        var response = await _chatClient.CompleteAsync(messages);
        return response.Content;
    }
}
```

### Phase 7: MCP Tools Implementation

**Status**: ⏳ 0% complete

MCP Tools to Implement:

**Character Tools**:
- [ ] `analyze_character` - Full character analysis
- [ ] `compare_to_top_players` - Compare with ladder

**Calculator Tools**:
- [ ] `calculate_character_ehp` - EHP breakdown
- [ ] `analyze_spirit_usage` - Spirit analysis
- [ ] `analyze_stun_vulnerability` - Stun mechanics
- [ ] `calculate_dps` - DPS calculations

**Analyzer Tools**:
- [ ] `detect_character_weaknesses` - Find build issues
- [ ] `evaluate_gear_upgrade` - Compare items
- [ ] `optimize_build_metrics` - Comprehensive optimization
- [ ] `check_content_readiness` - Content tier checker

**Optimizer Tools**:
- [ ] `optimize_gear` - Gear recommendations
- [ ] `optimize_passive_tree` - Passive suggestions
- [ ] `optimize_skills` - Skill setup optimization
- [ ] `find_best_supports` - Support gem combos

**AI Tools**:
- [ ] `natural_language_query` - AI-powered Q&A
- [ ] `explain_mechanic` - Game mechanics explanation
- [ ] `compare_items` - Item comparison

**Trade & Integration**:
- [ ] `search_trade_items` - Trade search
- [ ] `import_pob` - Path of Building import
- [ ] `export_pob` - Path of Building export

**Example Tool Implementation**:
```csharp
// In McpServer.cs
private void RegisterTools()
{
    _server.RegisterTool(new Tool
    {
        Name = "analyze_character",
        Description = "Analyze a PoE2 character",
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                account = new { type = "string" },
                character = new { type = "string" }
            },
            required = new[] { "account", "character" }
        },
        Handler = async (args) =>
        {
            var account = args["account"];
            var character = args["character"];
            return await _characterAnalyzer.AnalyzeAsync(account, character);
        }
    });
}
```

### Phase 8: Testing & Documentation

**Status**: ⏳ 10% complete

Tasks:
- [x] Create test project ✅
- [x] Sample calculator tests ✅
- [ ] Port remaining Python tests
- [ ] Create integration tests
- [ ] Add performance benchmarks
- [ ] Update README.md
- [ ] Create API documentation
- [ ] Write deployment guide
- [ ] Create Docker support

**Testing Strategy**:
```
Unit Tests:
- Calculators (100% coverage)
- Analyzers (90% coverage)
- Services (80% coverage)

Integration Tests:
- MCP server end-to-end
- Database operations
- External API calls (mocked)

Performance Tests:
- Calculator benchmarks
- API response times
- Memory usage profiling
```

## Key Differences: Python vs C#

### Configuration
| Python | C# |
|--------|-----|
| `config.yaml` | `appsettings.json` |
| `python-dotenv` | Built-in config system |
| Dictionary-based | Strongly-typed classes |

### Async/Await
| Python | C# |
|--------|-----|
| `async def func()` | `async Task<T> FuncAsync()` |
| `await func()` | `await FuncAsync()` |
| No cancellation tokens | `CancellationToken` support |

### Database
| Python | C# |
|--------|-----|
| SQLAlchemy | Entity Framework Core |
| Alembic migrations | EF Core migrations |
| `Session` context | `DbContext` |

### Dependency Injection
| Python | C# |
|--------|-----|
| Manual construction | Built-in DI container |
| Not type-safe | Full type safety |
| Limited scoping | Request/scoped/singleton |

### Type System
| Python | C# |
|--------|-----|
| Type hints (optional) | Strong typing (required) |
| Runtime checking | Compile-time checking |
| Duck typing | Interface/inheritance |

## Migration Patterns

### Pattern 1: Simple Function Port
```python
# Python
def calculate_resistance_multiplier(resistance: int) -> float:
    capped = min(resistance, 75)
    return 100.0 / (100.0 - capped)
```

```csharp
// C#
public static double CalculateResistanceMultiplier(int resistance)
{
    var capped = Math.Min(resistance, 75);
    return 100.0 / (100.0 - capped);
}
```

### Pattern 2: Async Service Port
```python
# Python
class CharacterFetcher:
    async def get_character(self, account, name):
        data = await self.api.fetch(account, name)
        return Character(data)
```

```csharp
// C#
public class CharacterFetcher : ICharacterFetcher
{
    private readonly IPoeApiClient _api;
    
    public async Task<Character> GetCharacterAsync(
        string account, 
        string name)
    {
        var data = await _api.FetchAsync(account, name);
        return new Character(data);
    }
}
```

### Pattern 3: Configuration Port
```python
# Python
settings = {
    "database": {
        "url": os.getenv("DATABASE_URL")
    }
}
```

```csharp
// C#
public class DatabaseOptions
{
    public string Url { get; set; } = "Data Source=poe2.db";
}

// In Startup/Program.cs
services.Configure<DatabaseOptions>(
    configuration.GetSection("Database"));
```

## Success Criteria

### Phase Completion
Each phase is complete when:
1. All tasks marked as done
2. All tests passing
3. Code reviewed
4. Documentation updated

### Final Success Metrics
- ✅ Solution builds with no warnings
- ✅ 100% test pass rate
- ✅ All MCP tools functional
- ✅ Performance equal or better than Python
- ✅ Full feature parity
- ✅ Documentation complete

## Timeline Estimate

- **Phase 1**: ✅ Complete (2 hours)
- **Phase 2**: 4-6 hours
- **Phase 3**: 6-8 hours
- **Phase 4**: 8-10 hours
- **Phase 5**: 6-8 hours
- **Phase 6**: 4-6 hours
- **Phase 7**: 10-12 hours
- **Phase 8**: 6-8 hours

**Total Estimate**: 46-60 hours

## Notes

### GitHub Copilot Custom Agent

A specialized **GitHub Copilot Custom Agent** has been created to assist with the Python to C# conversion. The agent is located at `.github/agents/convert-to-csharp.md` and provides:

- **Expert C# conversion guidance** - Specialized knowledge of Python to C# patterns
- **Project-specific context** - Understands this project structure and CONVERSION_PLAN.md
- **Code examples** - Comprehensive examples for common conversion patterns
- **Best practices enforcement** - Ensures .NET 10 and C# 14 best practices
- **Phase-aware assistance** - Knows which phase we're in and what to prioritize

**How to use the agent:**
1. In GitHub Copilot Chat, mention `@convert_to_csharp` to invoke the agent
2. Ask for help converting specific Python files or features
3. Request code reviews of converted C# code
4. Get guidance on MCP SDK usage, EF Core, or .NET patterns

**Example prompts:**
- `@convert_to_csharp help me convert the spirit calculator from Python to C#`
- `@convert_to_csharp review my EF Core DbContext implementation`
- `@convert_to_csharp what's the best way to implement this MCP tool?`

The agent has full access to the repository and understands the entire conversion plan, making it an invaluable assistant for this project.

### ModelContextProtocol SDK Research Needed
The C# SDK for MCP is relatively new. Key areas to research:
1. Server factory and initialization
2. Tool registration API
3. Stdio transport setup
4. Request/response handling
5. Error handling patterns

### Resources
- [C# MCP SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/)
- [Microsoft Learn - MCP Quickstart](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server)
- [.NET Blog - Build MCP Server](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)

### Python Code Preservation
The original Python implementation is preserved in `poe2-mcp-python/` for reference during conversion. This allows:
- Side-by-side comparison
- Reference for complex algorithms
- Fallback if needed
- Historical record

### Testing Strategy
- Unit tests for all calculators
- Integration tests for API clients
- End-to-end tests for MCP tools
- Performance benchmarks
- Code coverage targets (80%+)

## Current Status

**Overall Progress**: ~15% complete

**Recently Completed**:
- ✅ Project structure
- ✅ Solution setup
- ✅ NuGet packages
- ✅ EHP calculator with tests
- ✅ Python code migration
- ✅ GitHub Copilot Custom Agent for conversion assistance

**Next Steps**:
1. Research MCP SDK API
2. Implement proper MCP server
3. Create EF Core DbContext
4. Port remaining calculators
5. Implement API clients

**Blockers**: None

**Decisions Needed**: None

---

Last Updated: 2025-11-23
