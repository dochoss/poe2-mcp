# Path of Exile 2 Build Optimizer MCP - C# Edition

A comprehensive Model Context Protocol (MCP) server for Path of Exile 2 character optimization, featuring AI-powered build recommendations and intelligent upgrade suggestions.

## Overview

This is the C# .NET 10 version of the PoE2 Build Optimizer MCP server. The original Python implementation can be found in the `poe2-mcp-python` directory.

### Key Features

- **MCP Server**: Full implementation using ModelContextProtocol NuGet package
- **Character Analysis**: Fetch and analyze character data from PoE API
- **Build Optimization**: AI-powered recommendations for gear, passives, and skills
- **EHP Calculator**: Accurate Effective Health Pool calculations
- **Spirit System**: Analysis of PoE2's Spirit mechanic
- **Damage Calculations**: Comprehensive DPS breakdowns
- **Trade Integration**: Search for gear upgrades on the official trade site
- **AI Integration**: Natural language queries using Microsoft.Extensions.AI

## Technology Stack

- **.NET 10** (LTS Release)
- **C# 14**
- **ModelContextProtocol** - Official MCP SDK for .NET
- **Entity Framework Core 10** - Database ORM
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting
- **Microsoft.Extensions.AI** - AI service integration
- **SQLite** - Local database storage
- **xUnit** - Testing framework

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git

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
│   │   └── Tools/              # MCP tool implementations
│   └── Poe2Mcp.Core/          # Core business logic
│       ├── Calculators/        # EHP, damage, spirit calculators
│       ├── Analyzers/          # Build analyzers and optimizers
│       ├── Data/               # Database models and context
│       ├── Services/           # API clients and services
│       └── AI/                 # AI integration and query handling
├── tests/
│   └── Poe2Mcp.Tests/         # Unit and integration tests
├── poe2-mcp-python/           # Original Python implementation
└── Poe2Mcp.sln                # Solution file
```

## Building and Testing

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Publish for deployment
dotnet publish -c Release -o ./publish
```

## MCP Tools

The server exposes the following MCP tools:

- `analyze_character` - Analyze a PoE2 character
- `calculate_character_ehp` - Calculate Effective Health Pool
- `detect_character_weaknesses` - Find build weaknesses
- `optimize_build_metrics` - Comprehensive build optimization
- `search_trade_items` - Find gear upgrades on trade site
- `find_best_supports` - Optimal support gem combinations
- `explain_mechanic` - Explain PoE2 game mechanics
- `compare_items` - Compare two items
- `analyze_damage_scaling` - Identify damage bottlenecks
- `check_content_readiness` - Check if ready for specific content

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
