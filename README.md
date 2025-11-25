# Path of Exile 2 Build Optimizer MCP - C# Edition

A comprehensive Model Context Protocol (MCP) server for Path of Exile 2 character optimization, featuring AI-powered build recommendations and intelligent upgrade suggestions.

## Overview

This is the C# .NET 10 version of the PoE2 Build Optimizer MCP server. The original Python implementation can be found in the `poe2-mcp-python` directory.

### Key Features

- **MCP Server**: Full implementation using ModelContextProtocol v0.4.0-preview.3 with 27 tools
- **Character Analysis**: Fetch and analyze character data from PoE API
- **Build Optimization**: AI-powered recommendations for gear, passives, and skills
- **EHP Calculator**: Accurate Effective Health Pool calculations for all damage types
- **Spirit System**: Complete analysis of PoE2's Spirit mechanic with optimization
- **Damage Calculations**: Comprehensive DPS breakdowns with increased/more modifier support
- **Stun Calculator**: Light stun, heavy stun, and crushing blow mechanics
- **Trade Integration**: Search for gear upgrades on the official trade site
- **AI Integration**: Natural language queries using Microsoft.Extensions.AI
- **Game Mechanics**: Knowledge base with 6 comprehensive mechanics (freeze, shock, stun, crit, spirit, increased_vs_more)

### Implementation Status

**✅ Fully Implemented:**
- MCP Server infrastructure with 27 registered tools
- Core calculators: EHP, Spirit, Stun, Damage
- Mechanics knowledge base (6 game mechanics with detailed explanations)
- Data models and interfaces
- Database layer with Entity Framework Core + SQLite
- Service layer (API clients, caching, rate limiting)
- AI integration layer (supports OpenAI-compatible endpoints)
- Configuration and dependency injection
- 209 comprehensive unit and integration tests

**🚧 In Progress (Stub Implementations):**
- Tool implementations return placeholder responses with "Full implementation coming soon"
- Character data fetching from official API
- Build optimization algorithms
- Trade API integration
- Path of Building (PoB) import/export

See [CONVERSION_PLAN.md](CONVERSION_PLAN.md) for detailed migration roadmap.

## Documentation

- **[API Documentation](docs/API.md)** - Complete tool reference
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment instructions
- **[Conversion Plan](CONVERSION_PLAN.md)** - Python to C# migration status
- **[Phase 7 Summary](docs/PHASE_7_SUMMARY.md)** - Latest development phase notes

## Technology Stack

- **.NET 10** (LTS Release)
- **C# 14**
- **ModelContextProtocol v0.4.0-preview.3** - Official MCP SDK for .NET
- **Entity Framework Core 10** - Database ORM
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting
- **Microsoft.Extensions.AI v10.0.1-preview** - AI service integration
- **SQLite** - Local database storage
- **xUnit** - Testing framework (209 tests across 19 test classes)
- **OllamaSharp v3.0.8** - Local LLM integration
- **OpenAI v2.7.0** - OpenAI-compatible API support

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git
- Docker (optional, for containerized deployment)
- Local LLM server (optional, for AI features - see AI Configuration below)

### Installation

```bash
# Clone the repository
git clone https://github.com/dochoss/poe2-mcp.git
cd poe2-mcp

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the MCP server
cd src/Poe2Mcp.Server
dotnet run
```

### Docker Deployment

```bash
# Build and run with Docker
docker build -t poe2-mcp-server .
docker run -it -e AI__Endpoint=http://host.docker.internal:11434 poe2-mcp-server

# Or use Docker Compose
docker-compose up -d
```

See [Deployment Guide](docs/DEPLOYMENT.md) for detailed instructions.

### Configuration

Configuration is managed via `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Poe2Mcp": "Debug"
    }
  },
  "McpServer": {
    "Name": "poe2-build-optimizer",
    "Version": "1.0.0",
    "Description": "Path of Exile 2 Build Optimizer MCP Server"
  },
  "Database": {
    "ConnectionString": "Data Source=data/poe2_optimizer.db",
    "EnableSensitiveDataLogging": false
  },
  "Cache": {
    "DefaultExpirationMinutes": 60,
    "CharacterCacheMinutes": 5,
    "ApiCacheMinutes": 30
  },
  "AI": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3",
    "ApiKey": "",
    "Temperature": 0.7
  }
}
```

Create an `appsettings.Development.json` file for development-specific settings (this file is git-ignored):

```json
{
  "AI": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3",
    "ApiKey": "your-api-key-if-needed"
  },
  "TradeApi": {
    "PoeSessionId": "your-poe-session-id"
  }
}
```

### AI Configuration

The server supports OpenAI-compatible endpoints via `Microsoft.Extensions.AI`:

**Ollama (Local Development - Recommended):**
```bash
# Install and start Ollama
ollama serve

# Pull a model
ollama pull llama3

# Configure in appsettings.json
"AI": {
  "Endpoint": "http://localhost:11434",
  "Model": "llama3"
}
```

**OpenAI-Compatible Providers:**
```json
"AI": {
  "Endpoint": "https://api.openai.com/v1",
  "ApiKey": "sk-...",
  "Model": "gpt-4"
}
```

**Note:** Azure OpenAI and other providers are supported through the `Microsoft.Extensions.AI.OpenAI` package (already included).

## Project Structure

```
poe2-mcp/
├── src/
│   ├── Poe2Mcp.Server/        # MCP server application
│   │   ├── Program.cs          # Entry point and host configuration
│   │   ├── Tools/              # MCP tool implementations (27 tools)
│   │   │   ├── CharacterTools.cs    # Character analysis (2 tools)
│   │   │   ├── CalculatorTools.cs   # Calculators (4 tools)
│   │   │   ├── AnalyzerTools.cs     # Build analysis (5 tools)
│   │   │   ├── OptimizerTools.cs    # Optimizers (4 tools)
│   │   │   ├── AITools.cs           # AI queries (3 tools)
│   │   │   ├── UtilityTools.cs      # Trade, PoB, etc. (7 tools)
│   │   │   └── ServerTools.cs       # Health check, cache (2 tools)
│   │   ├── Configuration.cs         # Configuration models
│   │   └── AiServiceConfiguration.cs # AI setup
│   └── Poe2Mcp.Core/          # Core business logic
│       ├── Calculators/        # EHP, damage, spirit, stun calculators
│       ├── Analyzers/          # Build analyzers (weakness, gear, content readiness)
│       ├── Optimizers/         # Gear, passive, skill optimizers
│       ├── Data/               # Database models and context
│       ├── Services/           # API clients and services
│       ├── AI/                 # AI integration and query handling
│       │   ├── MechanicsKnowledgeBase.cs  # 6 game mechanics
│       │   ├── QueryHandler.cs
│       │   └── RecommendationEngine.cs
│       └── Models/             # DTOs and domain models
├── tests/
│   └── Poe2Mcp.Tests/         # Unit and integration tests (209 tests)
│       ├── Calculators/        # Calculator tests
│       ├── Analyzers/          # Analyzer tests
│       ├── Optimizers/         # Optimizer tests
│       ├── AI/                 # AI component tests (3 test classes)
│       ├── Services/           # Service tests
│       ├── Data/               # Database tests
│       └── Integration/        # Integration tests
├── benchmarks/
│   └── Poe2Mcp.Benchmarks/    # Performance benchmarks
├── docs/                       # Documentation
│   ├── API.md                  # Tool reference
│   ├── DEPLOYMENT.md           # Deployment guide
│   └── PHASE_7_SUMMARY.md      # Latest development notes
├── poe2-mcp-python/           # Original Python implementation
├── Dockerfile                  # Docker image definition
├── docker-compose.yml          # Docker Compose configuration
└── Poe2Mcp.sln                # Solution file
```

## Building and Testing

```bash
# Build the entire solution
dotnet build

# Run all tests (209 tests across 19 test classes)
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Run performance benchmarks
dotnet run --project benchmarks/Poe2Mcp.Benchmarks -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## MCP Tools (27 Total)

The server exposes 27 MCP tools organized into categories:

### Character Tools (2)
- `analyze_character` - Full character analysis
- `compare_to_top_players` - Compare with ladder

### Calculator Tools (4)
- `calculate_character_ehp` - Calculate Effective Health Pool ✅ **Implemented**
- `analyze_spirit_usage` - Spirit analysis ✅ **Implemented**
- `analyze_stun_vulnerability` - Stun mechanics ✅ **Implemented**
- `calculate_dps` - DPS calculations ✅ **Implemented**

### Analyzer Tools (5)
- `detect_character_weaknesses` - Find build issues
- `evaluate_gear_upgrade` - Compare items
- `optimize_build_metrics` - Comprehensive optimization
- `check_content_readiness` - Content tier checker
- `analyze_damage_scaling` - Damage scaling analysis

### Optimizer Tools (4)
- `optimize_gear` - Gear recommendations
- `optimize_passive_tree` - Passive suggestions
- `optimize_skills` - Skill setup optimization
- `find_best_supports` - Support gem combinations

### AI Tools (3)
- `natural_language_query` - AI-powered Q&A ✅ **Knowledge base ready**
- `explain_mechanic` - Game mechanics explanation ✅ **6 mechanics implemented**
- `compare_items` - AI item comparison

### Utility Tools (7)
- `search_trade_items` - Search trade market
- `import_pob` - Import Path of Building code
- `export_pob` - Export to Path of Building
- `get_pob_code` - Get PoB code from poe.ninja
- `setup_trade_auth` - Configure trade authentication
- `compare_builds` - Compare two builds
- `search_items` - Search game database

### Server Tools (2)
- `health_check` - Server status ✅ **Implemented**
- `clear_cache` - Cache management ✅ **Implemented**

See [API Documentation](docs/API.md) for complete tool reference with examples.

## Game Mechanics Knowledge Base

The server includes detailed explanations for 6 Path of Exile 2 mechanics:

1. **Freeze** (Ailment) - Cold damage immobilization mechanics
2. **Shock** (Ailment) - Lightning damage amplification
3. **Stun** (Crowd Control) - Light stun, Heavy stun, and Crushing Blow
4. **Critical Strike** (Damage) - Crit chance and multiplier calculations
5. **Spirit** (Resource) - PoE2's new aura resource system
6. **Increased vs More** (Scaling) - Understanding additive vs multiplicative modifiers

Each mechanic includes:
- Detailed explanation and how it works
- Calculation formulas with examples
- Common Q&A
- Changes from PoE1
- Related mechanics

## Development

### Code Style

This project follows standard C# coding conventions:
- PascalCase for public members
- camelCase for private fields
- async/await for asynchronous operations
- Dependency injection throughout
- Comprehensive XML documentation comments
- Primary constructors for concise dependency injection

### Testing

Tests are written using xUnit with FluentAssertions for readable assertions:

```csharp
[Fact]
public void CalculateResistanceMultiplier_WithMaxResist_Returns4x()
{
    // Arrange & Act
    var multiplier = EhpCalculator.CalculateResistanceMultiplier(75);
    
    // Assert
    multiplier.Should().Be(4.0);
}
```

**Test Coverage:**
- 209 tests across 19 test classes
- Unit tests for all calculators, analyzers, and optimizers
- AI component tests (knowledge base, query handler, recommendations)
- Service tests (caching, rate limiting, API clients)
- Integration tests (database, MCP server)

## Migrating from Python Version

If you're familiar with the Python version, here are the key differences:

| Python | C# |
|--------|-----|
| `config.yaml` | `appsettings.json` |
| `requirements.txt` | NuGet packages in `.csproj` |
| `async def` | `async Task<T>` |
| SQLAlchemy | Entity Framework Core |
| Type hints | Strong typing |
| pytest | xUnit |
| dict/dataclass | record types |

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

### Development Workflow

```bash
# Create a feature branch
git checkout -b feature/my-feature

# Make changes and test
dotnet test

# Build and verify
dotnet build

# Commit and push
git add .
git commit -m "Add my feature"
git push origin feature/my-feature
```

## License

Private project for personal use.

## Credits

Built with data from:
- [poe2db.tw](https://poe2db.tw) - Comprehensive PoE2 game data
- [poe.ninja](https://poe.ninja) - Meta builds and economy data
- Path of Exile 2 by Grinding Gear Games

## Comparison with Python Version

The original Python implementation is preserved in the `poe2-mcp-python` folder. The C# version provides:

- **Better Performance**: Compiled code and optimized runtime
- **Strong Typing**: Compile-time type safety with C# 14 features
- **Native Async**: First-class async/await support
- **Better Tooling**: Visual Studio, Rider, VS Code with OmniSharp
- **Enterprise Ready**: Production-grade DI, logging, configuration
- **Cross-Platform**: Runs on Windows, Linux, macOS via .NET
- **Modern Features**: Record types, primary constructors, nullable reference types

## Roadmap

See [CONVERSION_PLAN.md](CONVERSION_PLAN.md) for detailed conversion status.

**Next Priorities:**
1. Complete character fetching from official PoE API
2. Implement build optimization algorithms
3. Add trade API integration with session management
4. Path of Building import/export functionality
5. Expand mechanics knowledge base (10+ mechanics)
6. Performance optimization and benchmarking

---

**Built for the PoE2 community with .NET 10 and modern C# best practices**
