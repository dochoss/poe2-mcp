# Path of Exile 2 Build Optimizer MCP - C# Edition

A comprehensive Model Context Protocol (MCP) server for Path of Exile 2 character optimization, featuring AI-powered build recommendations and intelligent upgrade suggestions.

## Overview

This is the C# .NET 10 version of the PoE2 Build Optimizer MCP server. The original Python implementation can be found in the `poe2-mcp-python` directory.

### Key Features

- **MCP Server**: Full implementation using ModelContextProtocol NuGet package with 27 tools
- **Character Analysis**: Fetch and analyze character data from PoE API
- **Build Optimization**: AI-powered recommendations for gear, passives, and skills
- **EHP Calculator**: Accurate Effective Health Pool calculations for all damage types
- **Spirit System**: Complete analysis of PoE2's Spirit mechanic with optimization
- **Damage Calculations**: Comprehensive DPS breakdowns with increased/more modifier support
- **Stun Calculator**: Light stun, heavy stun, and crushing blow mechanics
- **Trade Integration**: Search for gear upgrades on the official trade site
- **AI Integration**: Natural language queries using Microsoft.Extensions.AI
- **Game Mechanics**: Knowledge base with 6 comprehensive mechanics (freeze, shock, stun, crit, spirit, increased_vs_more)

## Documentation

- **[API Documentation](docs/API.md)** - Complete tool reference
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment instructions
- **[Conversion Plan](CONVERSION_PLAN.md)** - Python to C# migration status

## Technology Stack

- **.NET 10** (LTS Release)
- **C# 14**
- **ModelContextProtocol v0.4.0** - Official MCP SDK for .NET
- **Entity Framework Core 10** - Database ORM
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting
- **Microsoft.Extensions.AI** - AI service integration
- **SQLite** - Local database storage
- **xUnit** - Testing framework (240+ tests)

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git
- Docker (optional, for containerized deployment)

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
docker run -it -e AI__ApiKey=your-key poe2-mcp-server

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
      "Microsoft": "Warning"
    }
  },
  "McpServer": {
    "Name": "poe2-build-optimizer",
    "Version": "1.0.0"
  },
  "Database": {
    "ConnectionString": "Data Source=poe2_optimizer.db"
  },
  "AI": {
    "Provider": "Anthropic",
    "Model": "claude-sonnet-4-20250514"
  }
}
```

Create an `appsettings.Development.json` file for development-specific settings (this file is git-ignored):

```json
{
  "AI": {
    "ApiKey": "your-api-key-here"
  },
  "Trade": {
    "SessionId": "your-poe-session-id"
  }
}
```

## Project Structure

```
poe2-mcp/
├── src/
│   ├── Poe2Mcp.Server/        # MCP server application
│   │   ├── Program.cs          # Entry point and host configuration
│   │   ├── McpServer.cs        # MCP server implementation
│   │   └── Tools/              # MCP tool implementations (27 tools)
│   └── Poe2Mcp.Core/          # Core business logic
│       ├── Calculators/        # EHP, damage, spirit, stun calculators
│       ├── Analyzers/          # Build analyzers (weakness, gear, content readiness)
│       ├── Optimizers/         # Gear, passive, skill optimizers
│       ├── Data/               # Database models and context
│       ├── Services/           # API clients and services
│       └── AI/                 # AI integration and query handling
├── tests/
│   └── Poe2Mcp.Tests/         # Unit and integration tests (240+ tests)
├── benchmarks/
│   └── Poe2Mcp.Benchmarks/    # Performance benchmarks
├── docs/                       # Documentation
│   ├── API.md                  # Tool reference
│   └── DEPLOYMENT.md           # Deployment guide
├── poe2-mcp-python/           # Original Python implementation
├── Dockerfile                  # Docker image definition
├── docker-compose.yml          # Docker Compose configuration
└── Poe2Mcp.sln                # Solution file
```

## Building and Testing

```bash
# Build the entire solution
dotnet build

# Run all tests (240+ tests)
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Run performance benchmarks
dotnet run --project benchmarks/Poe2Mcp.Benchmarks -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## MCP Tools

The server exposes 27 MCP tools organized into categories:

**Character Tools (2):**
- `analyze_character` - Full character analysis
- `compare_to_top_players` - Compare with ladder

**Calculator Tools (4):**
- `calculate_character_ehp` - Calculate Effective Health Pool
- `analyze_spirit_usage` - Spirit analysis
- `analyze_stun_vulnerability` - Stun mechanics
- `calculate_dps` - DPS calculations

**Analyzer Tools (4):**
- `detect_character_weaknesses` - Find build issues
- `evaluate_gear_upgrade` - Compare items
- `optimize_build_metrics` - Comprehensive optimization
- `check_content_readiness` - Content tier checker

**Optimizer Tools (4):**
- `optimize_gear` - Gear recommendations
- `optimize_passive_tree` - Passive suggestions
- `optimize_skills` - Skill setup optimization
- `find_best_supports` - Support gem combinations

**AI Tools (3):**
- `natural_language_query` - AI-powered Q&A
- `explain_mechanic` - Game mechanics explanation
- `compare_items` - AI item comparison

**Utility Tools (10):**
- `health_check`, `clear_cache`, `search_trade_items`, `import_pob`, `export_pob`, `get_pob_code`, `setup_trade_auth`, `compare_builds`, `search_items`, `analyze_damage_scaling`

See [API Documentation](docs/API.md) for complete tool reference.

## Development

### Code Style

This project follows standard C# coding conventions:
- PascalCase for public members
- camelCase for private fields
- async/await for asynchronous operations
- Dependency injection throughout
- Comprehensive XML documentation comments

### Testing

Tests are written using xUnit with FluentAssertions for readable assertions:

```csharp
[Fact]
public async Task CalculateEhp_WithValidStats_ReturnsAccurateEhp()
{
    // Arrange
    var stats = new DefensiveStats { Life = 5000, FireResistance = 75 };
    
    // Act
    var ehp = await _calculator.CalculateEhpAsync(stats);
    
    // Assert
    ehp.Physical.Should().BeGreaterThan(5000);
}
```

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

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

### Using the GitHub Copilot Custom Agent

This repository includes a specialized GitHub Copilot Custom Agent to assist with Python to C# conversion. Located at `.github/agents/convert-to-csharp.md`, the agent provides:

- Expert guidance on converting Python code to C# .NET 10
- Project-specific context and conversion patterns
- Best practices for MCP server development
- Code review and optimization suggestions

**To use the agent in GitHub Copilot:**
```
@convert_to_csharp help me convert the damage calculator to C#
@convert_to_csharp review this MCP tool implementation
@convert_to_csharp what's the best way to implement caching?
```

See `CONVERSION_PLAN.md` for the current conversion status and roadmap.

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
- **Strong Typing**: Compile-time type safety
- **Native Async**: First-class async/await support
- **Better Tooling**: Visual Studio, Rider, VS Code with OmniSharp
- **Enterprise Ready**: Production-grade DI, logging, configuration
- **Cross-Platform**: Runs on Windows, Linux, macOS via .NET

---

**Built for the PoE2 community with .NET 10 and modern C# best practices**
